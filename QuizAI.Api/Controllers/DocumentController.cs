using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;
using QuizAI.Api.Models;

namespace QuizAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DocumentController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetDocuments()
    {
        var docs = await _context.Documents
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.MimeType,
                d.FileSize,
                d.Processed,
                d.UploadedAt,
                ChunkCount = d.Chunks.Count
            })
            .ToListAsync();
        return Ok(docs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetDocument(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null) return NotFound();
        return doc;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsDir, uniqueName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var document = new Document
        {
            FileName = file.FileName,
            MimeType = file.ContentType,
            FileSize = file.Length,
            StorageUrl = filePath,
            OwnerId = Guid.Empty,
            Processed = false
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, new
        {
            document.Id,
            document.FileName,
            document.MimeType,
            document.FileSize,
            document.Processed,
            document.UploadedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null) return NotFound();

        if (System.IO.File.Exists(doc.StorageUrl))
            System.IO.File.Delete(doc.StorageUrl);

        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
