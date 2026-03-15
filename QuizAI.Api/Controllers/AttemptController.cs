using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;
using QuizAI.Api.Models;
using QuizAI.Api.Services;

namespace QuizAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttemptController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly OpenAIService _openAIService;

    public AttemptController(AppDbContext context, OpenAIService openAIService)
    {
        _context = context;
        _openAIService = openAIService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAttempt([FromBody] StartAttemptDto dto)
    {
        var userId = GetUserId();

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId);

        if (quiz == null) return NotFound(new { message = "Quiz not found" });
        if (!quiz.Published) return BadRequest(new { message = "Quiz is not published" });

        var attempt = new QuizAttempt
        {
            QuizId = dto.QuizId,
            UserId = userId,
            Status = "in_progress",
            MaxTotalScore = quiz.Questions.Sum(q => q.MaxScore)
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResult), new { id = attempt.Id }, new
        {
            attempt.Id,
            attempt.QuizId,
            attempt.StartedAt,
            attempt.Status,
            attempt.MaxTotalScore
        });
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitAttempt(Guid id, [FromBody] SubmitAttemptDto dto)
    {
        var userId = GetUserId();

        var attempt = await _context.QuizAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (attempt == null) return NotFound(new { message = "Attempt not found" });
        if (attempt.Status != "in_progress") return BadRequest(new { message = "Attempt already submitted" });

        var questions = await _context.Questions
            .Include(q => q.Options)
            .Where(q => q.QuizId == attempt.QuizId)
            .ToListAsync();

        decimal totalScore = 0;

        foreach (var answer in dto.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            decimal? autoScore = null;
            string? feedbackJson = null;

            if (question.Type == "mcq" || question.Type == "true_false")
            {
                var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId);
                autoScore = selectedOption?.IsCorrect == true ? question.MaxScore : 0;
            }
            else if (question.Type == "fill_blank" && !string.IsNullOrWhiteSpace(answer.AnswerText))
            {
                var correctAnswer = (question.RubricJson ?? string.Empty).Trim().ToLowerInvariant();
                var studentAnswer = answer.AnswerText.Trim().ToLowerInvariant();
                autoScore = studentAnswer == correctAnswer ? question.MaxScore : 0;
                feedbackJson = autoScore > 0
                    ? "{\"feedback\":\"Correct!\"}"
                    : $"{{\"feedback\":\"Incorrect. The correct answer is: {question.RubricJson}\"}}";
            }
            else if ((question.Type == "essay" || question.Type == "short_answer" || question.Type == "long_answer") && !string.IsNullOrWhiteSpace(answer.AnswerText))
            {
                var rubric = question.RubricJson ?? "Grade based on accuracy and completeness.";
                feedbackJson = await _openAIService.GradeEssay(question.Prompt, answer.AnswerText, rubric);
            }

            if (autoScore.HasValue) totalScore += autoScore.Value;

            _context.AttemptAnswers.Add(new AttemptAnswer
            {
                AttemptId = attempt.Id,
                QuestionId = answer.QuestionId,
                SelectedOptionId = answer.SelectedOptionId,
                AnswerText = answer.AnswerText,
                AutoScore = autoScore,
                FeedbackJson = feedbackJson,
                GradedAt = DateTime.UtcNow
            });
        }

        attempt.TotalScore = totalScore;
        attempt.Status = "graded";
        attempt.FinishedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            attempt.Id,
            attempt.TotalScore,
            attempt.MaxTotalScore,
            attempt.Status,
            attempt.FinishedAt
        });
    }

    [HttpGet("{id}/result")]
    public async Task<IActionResult> GetResult(Guid id)
    {
        var userId = GetUserId();

        var attempt = await _context.QuizAttempts
            .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOption)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (attempt == null) return NotFound(new { message = "Attempt not found" });

        return Ok(new
        {
            attempt.Id,
            attempt.QuizId,
            attempt.TotalScore,
            attempt.MaxTotalScore,
            attempt.Status,
            attempt.StartedAt,
            attempt.FinishedAt,
            Answers = attempt.Answers.Select(a => new
            {
                a.Id,
                QuestionPrompt = a.Question.Prompt,
                QuestionType = a.Question.Type,
                a.AnswerText,
                SelectedOption = a.SelectedOption == null ? null : new
                {
                    a.SelectedOption.Content,
                    a.SelectedOption.IsCorrect
                },
                CorrectOption = a.Question.Options.FirstOrDefault(o => o.IsCorrect) == null ? null : new
                {
                    a.Question.Options.First(o => o.IsCorrect).Content
                },
                a.AutoScore,
                FinalScore = a.ManualScore ?? a.AutoScore ?? 0,
                MaxScore = a.Question.MaxScore,
                a.FeedbackJson
            })
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAttempts()
    {
        var userId = GetUserId();

        var attempts = await _context.QuizAttempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => new
            {
                a.Id,
                a.QuizId,
                QuizTitle = a.Quiz.Title,
                a.TotalScore,
                a.MaxTotalScore,
                a.Status,
                a.StartedAt,
                a.FinishedAt
            })
            .ToListAsync();

        return Ok(attempts);
    }

    private Guid GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}

public record StartAttemptDto(Guid QuizId);

public record AnswerItemDto(
    Guid QuestionId,
    Guid? SelectedOptionId,
    string? AnswerText
);

public record SubmitAttemptDto(List<AnswerItemDto> Answers);
