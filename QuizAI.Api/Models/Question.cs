namespace QuizAI.Api.Models;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public int Seq { get; set; }
    public string Type { get; set; } = "mcq";
    public string Prompt { get; set; } = string.Empty;
    public decimal MaxScore { get; set; } = 1;
    public string? RubricJson { get; set; }
    public string? SourceChunkIdsJson { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
