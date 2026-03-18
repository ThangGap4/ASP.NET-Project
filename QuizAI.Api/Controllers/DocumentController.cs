using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAI.Api.Data;
using QuizAI.Api.Models;
using QuizAI.Api.Services;

namespace QuizAI.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly DocumentProcessorService _processor;
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentController(
        AppDbContext context,
        IWebHostEnvironment env,
        DocumentProcessorService processor,
        IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _env = env;
        _processor = processor;
        _scopeFactory = scopeFactory;
    }

    // GET /api/documents – Lấy danh sách documents của user hiện tại
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetDocuments()
    {
        var userId = GetUserId();
        var docs = await _context.Documents
            .Where(d => d.OwnerId == userId)
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
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        return Ok(docs);
    }

    // GET /api/documents/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetDocument(Guid id)
    {
        var userId = GetUserId();
        var doc = await _context.Documents
            .Where(d => d.Id == id && d.OwnerId == userId)
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.MimeType,
                d.FileSize,
                d.Processed,
                d.UploadedAt,
                d.StorageUrl,
                ChunkCount = d.Chunks.Count
            })
            .FirstOrDefaultAsync();

        if (doc == null) return NotFound(new { message = "Document not found" });
        return Ok(doc);
    }

    // POST /api/documents/upload – Upload file (txt, pdf, docx)
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileDto dto)
    {
        var file = dto.File;
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File is required" });

        var allowedExtensions = new[] { ".txt", ".pdf", ".docx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "Only .txt, .pdf, .docx files are allowed" });

        var userId = GetUserId();
        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", userId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var uniqueName = $"{Guid.NewGuid()}{ext}";
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
            OwnerId = userId,
            Processed = false
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Process in background using a new DI scope (DocumentProcessorService is Scoped)
        var docId = document.Id;
        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<DocumentProcessorService>();
            try { await processor.ProcessDocumentAsync(docId); }
            catch { /* logged inside service */ }
        });

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, new
        {
            document.Id,
            document.FileName,
            document.MimeType,
            document.FileSize,
            document.Processed,
            document.UploadedAt,
            message = "File uploaded. Processing in background..."
        });
    }

    // POST /api/documents/upload-url – Import từ URL web
    [HttpPost("upload-url")]
    public async Task<IActionResult> UploadFromUrl([FromBody] UploadUrlDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Url))
            return BadRequest(new { message = "URL is required" });

        if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
            return BadRequest(new { message = "Invalid URL" });

        var userId = GetUserId();

        var document = new Document
        {
            FileName = dto.Title ?? uri.Host + uri.AbsolutePath,
            MimeType = "text/html",
            FileSize = 0,
            StorageUrl = dto.Url,    // Store the URL directly
            OwnerId = userId,
            Processed = false
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Process in background using a new DI scope
        var docId2 = document.Id;
        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<DocumentProcessorService>();
            try { await processor.ProcessDocumentAsync(docId2); }
            catch { /* logged inside service */ }
        });

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, new
        {
            document.Id,
            document.FileName,
            document.MimeType,
            document.Processed,
            document.UploadedAt,
            message = "URL received. Processing in background..."
        });
    }

    // GET /api/documents/{id}/chunks – Xem danh sách chunks (để debug/xem trước)
    [HttpGet("{id}/chunks")]
    public async Task<IActionResult> GetChunks(Guid id)
    {
        var userId = GetUserId();
        var doc = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);
        if (doc == null) return NotFound(new { message = "Document not found" });

        var chunks = await _context.DocumentChunks
            .Where(c => c.DocumentId == id)
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new
            {
                c.Id,
                c.ChunkIndex,
                c.TokenCount,
                ContentPreview = c.Content.Substring(0, c.Content.Length > 200 ? 200 : c.Content.Length),
                HasEmbedding = c.EmbeddingVector != null
            })
            .ToListAsync();

        return Ok(chunks);
    }

    // DELETE /api/documents/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var userId = GetUserId();
        var doc = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);
        if (doc == null) return NotFound(new { message = "Document not found" });

        if (!doc.StorageUrl.StartsWith("http"))
        {
            if (System.IO.File.Exists(doc.StorageUrl))
                System.IO.File.Delete(doc.StorageUrl);
        }

        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/documents/upload-text – Paste nội dung trực tiếp
    [HttpPost("upload-text")]
    public async Task<IActionResult> UploadText([FromBody] UploadTextDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Content is required" });

        var userId = GetUserId();
        var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", userId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var fileName = string.IsNullOrWhiteSpace(dto.Title) ? "pasted-text.txt" : $"{dto.Title.Trim()}.txt";
        var uniqueName = $"{Guid.NewGuid()}.txt";
        var filePath = Path.Combine(uploadsDir, uniqueName);

        await System.IO.File.WriteAllTextAsync(filePath, dto.Content, System.Text.Encoding.UTF8);

        var document = new Document
        {
            FileName = fileName,
            MimeType = "text/plain",
            FileSize = System.Text.Encoding.UTF8.GetByteCount(dto.Content),
            StorageUrl = filePath,
            OwnerId = userId,
            Processed = false
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var docId = document.Id;
        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<DocumentProcessorService>();
            try { await processor.ProcessDocumentAsync(docId); }
            catch { }
        });

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, new
        {
            document.Id,
            document.FileName,
            document.MimeType,
            document.Processed,
            document.UploadedAt,
            message = "Text saved. Processing in background..."
        });
    }

    // PATCH /api/documents/{id} – Đổi tên hoặc cập nhật nội dung
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentDto dto)
    {
        var userId = GetUserId();
        var doc = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId);
        if (doc == null) return NotFound(new { message = "Document not found" });

        if (!string.IsNullOrWhiteSpace(dto.FileName))
            doc.FileName = dto.FileName.Trim();

        bool reprocess = false;
        if (!string.IsNullOrWhiteSpace(dto.Content))
        {
            if (doc.StorageUrl.StartsWith("http"))
                return BadRequest(new { message = "Cannot edit content of URL documents" });

            await System.IO.File.WriteAllTextAsync(doc.StorageUrl, dto.Content, System.Text.Encoding.UTF8);
            doc.FileSize = System.Text.Encoding.UTF8.GetByteCount(dto.Content);
            doc.Processed = false;

            var existingChunks = _context.DocumentChunks.Where(c => c.DocumentId == id);
            _context.DocumentChunks.RemoveRange(existingChunks);
            reprocess = true;
        }

        await _context.SaveChangesAsync();

        if (reprocess)
        {
            _ = Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<DocumentProcessorService>();
                try { await processor.ProcessDocumentAsync(id); }
                catch { }
            });
        }

        return Ok(new { doc.Id, doc.FileName, doc.Processed, message = reprocess ? "Content updated. Re-processing..." : "Updated." });
    }

    private Guid GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
    }
}

public record UploadUrlDto(string Url, string? Title);
public record UploadTextDto(string Content, string? Title);
public record UpdateDocumentDto(string? FileName, string? Content);

public class UploadFileDto
{
    public IFormFile File { get; set; } = null!;
}
