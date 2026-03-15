namespace QuizAI.Api.Models;

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public int OptIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;

    public Question Question { get; set; } = null!;
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
