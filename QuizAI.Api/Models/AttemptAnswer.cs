namespace QuizAI.Api.Models;

public class AttemptAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? AnswerText { get; set; }
    public decimal? AutoScore { get; set; }
    public decimal? ManualScore { get; set; }
    public string? FeedbackJson { get; set; }
    public DateTime? GradedAt { get; set; }

    public QuizAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public QuestionOption? SelectedOption { get; set; }
}
