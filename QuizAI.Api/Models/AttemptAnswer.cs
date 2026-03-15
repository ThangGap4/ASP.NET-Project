namespace QuizAI.Api.Models;

public class AttemptAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string Answer { get; set; }
    public bool IsCorrect { get; set; }
    
    public QuizAttempt Attempt { get; set; }
}
