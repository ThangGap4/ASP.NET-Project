using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;
using QuizAI.Api.Models;

namespace QuizAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuizController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetQuizzes()
    {
        var quizzes = await _context.Quizzes
            .Select(q => new
            {
                q.Id,
                q.Title,
                q.Description,
                q.Published,
                q.CreatedAt,
                QuestionCount = q.Questions.Count
            })
            .ToListAsync();
        return Ok(quizzes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetQuiz(Guid id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null) return NotFound();

        return Ok(new
        {
            quiz.Id,
            quiz.Title,
            quiz.Description,
            quiz.Published,
            quiz.CreatedAt,
            Questions = quiz.Questions.OrderBy(q => q.Seq).Select(q => new
            {
                q.Id,
                q.Seq,
                q.Type,
                q.Prompt,
                q.MaxScore,
                Options = q.Options.OrderBy(o => o.OptIndex).Select(o => new
                {
                    o.Id,
                    o.OptIndex,
                    o.Content
                })
            })
        });
    }

    [HttpPost]
    public async Task<ActionResult<Quiz>> CreateQuiz([FromBody] CreateQuizDto dto)
    {
        var quiz = new Quiz
        {
            Title = dto.Title,
            Description = dto.Description,
            SourceDocumentId = dto.SourceDocumentId,
            CreatorId = null
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, quiz);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(Guid id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null) return NotFound();

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateQuizDto(string Title, string? Description, Guid? SourceDocumentId);
