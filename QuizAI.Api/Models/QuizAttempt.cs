namespace QuizAI.Api.Models;

public class QuizAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public decimal Score { get; set; }
    
    public Quiz Quiz { get; set; }
    public AppUser User { get; set; }
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
