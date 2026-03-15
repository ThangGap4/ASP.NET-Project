namespace QuizAI.Api.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; }
    public string Content { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
    
    public AppUser User { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
