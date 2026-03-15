namespace QuizAI.Api.Models;

public class QuizAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "in_progress";
    public decimal TotalScore { get; set; } = 0;
    public decimal MaxTotalScore { get; set; } = 0;

    public Quiz Quiz { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
