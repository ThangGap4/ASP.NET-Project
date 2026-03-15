namespace QuizAI.Api.Models;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string Content { get; set; }
    public int ChunkIndex { get; set; }
    public Dictionary<string, object> Embedding { get; set; } = new();
    
    public Document Document { get; set; }
}
