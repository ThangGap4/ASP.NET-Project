namespace QuizAI.Api.Models;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public string Content { get; set; }
    public string Type { get; set; }
    public int Order { get; set; }
    
    public Quiz Quiz { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}
