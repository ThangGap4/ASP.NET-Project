namespace QuizAI.Api.Models;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; }
    public string Description { get; set; }
    public Guid UserId { get; set; }
    public Guid DocumentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public AppUser User { get; set; }
    public Document Document { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
