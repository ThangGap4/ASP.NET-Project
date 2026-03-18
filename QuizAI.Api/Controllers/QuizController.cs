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

    // GET /api/quizzes – Lấy danh sách quiz của user
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetQuizzes()
    {
        var userId = GetUserId();
        var quizzes = await _context.Quizzes
            .Where(q => q.CreatorId == userId || q.Published)
            .Select(q => new
            {
                q.Id,
                q.Title,
                q.Description,
                q.Published,
                q.CreatedAt,
                q.SourceDocumentId,
                QuestionCount = q.Questions.Count,
                IsOwner = q.CreatorId == userId
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
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id && (q.CreatorId == userId || q.Published));

        if (quiz == null) return NotFound(new { message = "Quiz not found" });

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
                // Hide is_correct from students
                Options = q.Options.OrderBy(o => o.OptIndex).Select(o => new
                {
                    o.Id,
                    o.OptIndex,
                    o.Content
                    // is_correct intentionally hidden
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
            // Strip markdown code blocks if present
            var cleanJson = rawJson.Trim();
            if (cleanJson.StartsWith("```")) {
                cleanJson = cleanJson.Replace("```json", "").Replace("```", "").Trim();
            }
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
