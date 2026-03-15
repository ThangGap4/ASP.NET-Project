using Microsoft.AspNetCore.Mvc;
using QuizAI.Api.Data;
using QuizAI.Api.Models;

namespace QuizAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly AppDbContext _context;

    public DocumentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
    {
        return await System.Threading.Tasks.Task.FromResult(_context.Documents.ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetDocument(System.Guid id)
    {
        var doc = await System.Threading.Tasks.Task.FromResult(_context.Documents.FirstOrDefault(d => d.Id == id));
        if (doc == null)
            return NotFound();
        return doc;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Document>> UploadDocument([FromForm] UploadDocumentDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest("File is required");

        using var reader = new System.IO.StreamReader(dto.File.OpenReadStream());
        var content = await reader.ReadToEndAsync();

        var document = new Document
        {
            FileName = dto.File.FileName,
            Content = content,
            UserId = System.Guid.NewGuid()
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(System.Guid id)
    {
        var doc = await System.Threading.Tasks.Task.FromResult(_context.Documents.FirstOrDefault(d => d.Id == id));
        if (doc == null)
            return NotFound();

        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record UploadDocumentDto(IFormFile File);
