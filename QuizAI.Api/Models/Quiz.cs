namespace QuizAI.Api.Models;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CreatorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public bool Published { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? Creator { get; set; }
    public Document? SourceDocument { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
