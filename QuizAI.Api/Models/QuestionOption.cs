namespace QuizAI.Api.Models;

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public string Content { get; set; }
    public bool IsCorrect { get; set; }
    
    public Question Question { get; set; }
}
