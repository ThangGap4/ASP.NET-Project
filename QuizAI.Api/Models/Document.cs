namespace QuizAI.Api.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public bool Processed { get; set; } = false;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public AppUser Owner { get; set; } = null!;
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public ICollection<Quiz> GeneratedQuizzes { get; set; } = new List<Quiz>();
}
