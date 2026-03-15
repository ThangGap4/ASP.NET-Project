using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<IEnumerable<Quiz>>> GetQuizzes()
    {
        return await System.Threading.Tasks.Task.FromResult(_context.Quizzes.ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Quiz>> GetQuiz(System.Guid id)
    {
        var quiz = await System.Threading.Tasks.Task.FromResult(_context.Quizzes.FirstOrDefault(q => q.Id == id));
        if (quiz == null)
            return NotFound();
        return quiz;
    }

    [HttpPost]
    public async Task<ActionResult<Quiz>> CreateQuiz([FromBody] CreateQuizDto dto)
    {
        var quiz = new Quiz
        {
            Title = dto.Title,
            Description = dto.Description,
            UserId = System.Guid.NewGuid(),
            DocumentId = dto.DocumentId
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, quiz);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(System.Guid id)
    {
        var quiz = await System.Threading.Tasks.Task.FromResult(_context.Quizzes.FirstOrDefault(q => q.Id == id));
        if (quiz == null)
            return NotFound();

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateQuizDto(string Title, string Description, System.Guid DocumentId);
