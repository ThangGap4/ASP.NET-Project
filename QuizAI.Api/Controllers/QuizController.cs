using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;
using QuizAI.Api.Models;
using QuizAI.Api.Services;

namespace QuizAI.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly OpenAIService _openAIService;
    private readonly EmbeddingService _embeddingService;

    public QuizController(AppDbContext context, OpenAIService openAIService, EmbeddingService embeddingService)
    {
        _context = context;
        _openAIService = openAIService;
        _embeddingService = embeddingService;
    }

    // GET /api/quizzes – Lấy danh sách quiz của user (chỉ lấy quiz của TÔI, không lấy của người khác)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetQuizzes()
    {
        var userId = GetUserId();
        var quizzes = await _context.Quizzes
            .AsNoTracking()
            .Where(q => q.CreatorId == userId)
            .Select(q => new
            {
                q.Id,
                q.Title,
                q.Description,
                q.Published,
                q.CreatedAt,
                q.SourceDocumentId,
                QuestionCount = q.Questions.Count,
                IsOwner = true
            })
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return Ok(quizzes);
    }

    // GET /api/quizzes/{id} – Chi tiết quiz (ẩn is_correct)
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetQuiz(Guid id)
    {
        var userId = GetUserId();
        var quiz = await _context.Quizzes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id && (q.CreatorId == userId || q.Published));

        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        var isCreator = quiz.CreatorId == userId;

        return Ok(new
        {
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.Published,
            quiz.CreatedAt,
            quiz.SourceDocumentId,
            Questions = quiz.Questions.OrderBy(q => q.Seq).Select(q => new
            {
                q.Id,
                q.Seq,
                q.Type,
                q.Prompt,
                q.MaxScore,
                Rubric = isCreator ? q.RubricJson : null,
                Options = q.Options.OrderBy(o => o.OptIndex).Select(o => new
                {
                    o.Id,
                    o.OptIndex,
                    o.Content,
                    IsCorrect = isCreator ? (bool?)o.IsCorrect : null
                })
            })
        });
    }

    // POST /api/quizzes/generate – Sinh quiz từ document qua OpenAI
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQuiz([FromBody] GenerateQuizDto dto)
    {
        var userId = GetUserId();

        // Verify document belongs to user
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == dto.DocumentId && d.OwnerId == userId);

        if (document == null)
            return NotFound(new { message = "Document not found" });

        if (!document.Processed)
            return BadRequest(new { message = "Document is still being processed. Please wait." });

        // Get top-k relevant chunks using RAG
        var topChunks = await _embeddingService.GetTopKChunksAsync(
            dto.DocumentId,
            query: $"generate {dto.QuestionCount} quiz questions difficulty: {dto.Difficulty}",
            k: Math.Min(8, dto.QuestionCount + 3)
        );

        if (!topChunks.Any())
            return BadRequest(new { message = "No content found in document to generate questions from." });

        // Build context from chunks
        var context = string.Join("\n\n", topChunks.Select((c, i) =>
            $"[CHUNK_{c.ChunkIndex}]\n{c.Content}"));

        // Call OpenAI
        string rawJson;
        try
        {
            rawJson = await _openAIService.GenerateQuizJson(context, dto.QuestionCount, dto.Difficulty, dto.QuestionType ?? "mcq");
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "AI service error", detail = ex.Message });
        }

        // Parse JSON
        QuizJsonResponse? parsed;
        try
        {
            // Strip markdown code blocks if present just in case
            var cleanJson = rawJson.Trim();
            if (cleanJson.StartsWith("```")) {
                cleanJson = cleanJson.Replace("```json", "").Replace("```", "").Trim();
            }
            
            // Console.WriteLine for debugging
            Console.WriteLine("--- RAW AI JSON ---");
            Console.WriteLine(cleanJson);
            Console.WriteLine("-------------------");

            parsed = JsonSerializer.Deserialize<QuizJsonResponse>(cleanJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Failed to parse AI response", detail = ex.Message, raw = rawJson });
        }

        if (parsed?.Questions == null || !parsed.Questions.Any())
            return StatusCode(502, new { message = "AI returned no questions", raw = rawJson });

        // Save Quiz + Questions + Options
        var quiz = new Quiz
        {
            Title = dto.Title ?? $"Quiz from {document.FileName}",
            Description = dto.Description ?? $"Generated quiz ({dto.Difficulty}, {dto.QuestionCount} questions)",
            SourceDocumentId = dto.DocumentId,
            CreatorId = userId,
            Published = false
        };
        _context.Quizzes.Add(quiz);

        int seq = 1;
        foreach (var q in parsed.Questions)
        {
            var question = new Question
            {
                QuizId = quiz.Id,
                Seq = seq++,
                Type = q.Type ?? "mcq",
                Prompt = q.Prompt,
                MaxScore = 1,
                RubricJson = q.Rubric
            };
            _context.Questions.Add(question);

            if (q.Options != null)
            {
                int optIdx = 0;
                foreach (var opt in q.Options)
                {
                    _context.QuestionOptions.Add(new QuestionOption
                    {
                        QuestionId = question.Id,
                        OptIndex = optIdx++,
                        Content = opt.Content,
                        IsCorrect = opt.IsCorrect
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, new
        {
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.Published,
            quiz.CreatedAt,
            QuestionCount = parsed.Questions.Count
        });
    }

    // PATCH /api/quizzes/{id}/publish – Publish/unpublish quiz
    [HttpPatch("{id}/publish")]
    public async Task<IActionResult> PublishQuiz(Guid id, [FromBody] PublishDto dto)
    {
        var userId = GetUserId();
        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == id && q.CreatorId == userId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        quiz.Published = dto.Published;
        await _context.SaveChangesAsync();
        return Ok(new { quiz.Id, quiz.Published });
    }

    // DELETE /api/quizzes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(Guid id)
    {
        var userId = GetUserId();
        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == id && q.CreatorId == userId);
        if (quiz == null) return NotFound(new { message = "Quiz not found" });

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/quizzes/{id}/statistics
    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetQuizStatistics(Guid id)
    {
        var userId = GetUserId();
        
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(q => q.Id == id && q.CreatorId == userId);
            
        if (quiz == null) return NotFound(new { message = "Quiz not found or unauthorized" });
        
        var attempts = await _context.QuizAttempts
            .Where(a => a.QuizId == id && a.Status == "graded" && a.MaxTotalScore > 0)
            .Include(a => a.Answers)
            .AsNoTracking()
            .ToListAsync();
            
        var totalAttempts = attempts.Count;
        var avgScore = totalAttempts > 0 
            ? attempts.Average(a => (double)a.TotalScore / (double)a.MaxTotalScore * 100) 
            : 0;
            
        var allAnswers = attempts.SelectMany(a => a.Answers).ToList();
        
        var questionsStat = quiz.Questions.OrderBy(q => q.Seq).Select(q => {
            var qAnswers = allAnswers.Where(a => a.QuestionId == q.Id).ToList();
            var qTotal = qAnswers.Count;
            var qCorrect = qAnswers.Count(a => a.AutoScore == q.MaxScore || a.AutoScore > 0);
            
            return new QuestionStatisticsDto
            (
                q.Id,
                q.Prompt,
                q.Type,
                qTotal,
                qCorrect,
                qTotal > 0 ? Math.Round((double)qCorrect / qTotal * 100, 1) : 0,
                q.Options.OrderBy(o => o.OptIndex).Select(o => {
                    var selectCount = qAnswers.Count(a => a.SelectedOptionId == o.Id);
                    return new OptionStatisticsDto
                    (
                        o.Id,
                        o.Content,
                        o.IsCorrect,
                        selectCount,
                        qTotal > 0 ? Math.Round((double)selectCount / qTotal * 100, 1) : 0
                    );
                }).ToList()
            );
        }).ToList();
        
        return Ok(new QuizStatisticsDto
        (
            totalAttempts,
            Math.Round(avgScore, 1),
            questionsStat
        ));
    }

    [HttpGet("{id}/explain-question/{questionId}")]
    public async Task<IActionResult> ExplainQuestion(Guid id, Guid questionId)
    {
        var userId = GetUserId();
        
        var quiz = await _context.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id && (q.CreatorId == userId || q.Published));
            
        if (quiz == null) return NotFound(new { message = "Quiz not found or unauthorized" });
        
        var question = await _context.Questions
            .Include(q => q.Options)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId && q.QuizId == id);
            
        if (question == null) return NotFound(new { message = "Question not found" });

        var sourceDocumentId = quiz.SourceDocumentId;
        if (sourceDocumentId == null) 
            return BadRequest(new { message = "This quiz is not linked to any source document, so AI cannot extract explanations from context." });

        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect)?.Content;
        if (question.Type == "fill_blank" || question.Type == "essay" || question.Type == "short_answer" || question.Type == "long_answer")
        {
            correctOption = question.RubricJson;
        }

        var prompt = question.Prompt;
        var queryContext = $"{prompt}\n{correctOption}";
        
        var chunks = await _embeddingService.GetTopKChunksAsync(sourceDocumentId.Value, queryContext, 3);
        
        if (!chunks.Any())
            return NotFound(new { message = "Could not find relevant context in the source document." });

        var contextText = string.Join("\n\n", chunks.Select(c => c.Content));

        try
        {
            var explanationJson = await _openAIService.ExplainAnswerAsync(prompt, null, correctOption, contextText);
            return Content(explanationJson, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Failed to generate explanation from AI", detail = ex.Message });
        }
    }

    private Guid GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}

// DTOs
public record CreateQuizDto(string Title, string? Description, Guid? SourceDocumentId);
public record GenerateQuizDto(
    Guid DocumentId,
    int QuestionCount,
    string Difficulty,
    string? Title,
    string? Description,
    string? QuestionType
);
public record PublishDto(bool Published);

// OpenAI JSON response models
public class QuizJsonResponse
{
    public List<QuizQuestionJson> Questions { get; set; } = new();
}
public class QuizQuestionJson
{
    public string Prompt { get; set; } = string.Empty;
    public string? Type { get; set; }
    public List<QuizOptionJson>? Options { get; set; }
    public string? Rubric { get; set; }
}
public class QuizOptionJson
{
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public record QuizStatisticsDto(
    int TotalParticipants,
    double AverageScorePercent,
    List<QuestionStatisticsDto> Questions
);

public record QuestionStatisticsDto(
    Guid QuestionId,
    string Prompt,
    string Type,
    int TotalAnswers,
    int CorrectAnswers,
    double CorrectRatePercent,
    List<OptionStatisticsDto> Options
);

public record OptionStatisticsDto(
    Guid OptionId,
    string Content,
    bool IsCorrect,
    int SelectionCount,
    double SelectionRatePercent
);
