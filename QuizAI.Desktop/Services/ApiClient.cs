namespace QuizAI.Desktop.Services;

public class ApiClient
{
    private readonly string _baseUrl = "http://localhost:5127/api";

    public async Task<List<QuizDto>> GetQuizzesAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            var response = await client.GetAsync($"{_baseUrl}/quizzes");
            return [];
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }
        return [];
    }

    public async Task<Guid> CreateQuizAsync(string title, string description)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            return System.Guid.NewGuid();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: {ex.Message}");
        }
        return System.Guid.Empty;
    }
}

public record QuizDto(
    System.Guid Id,
    string Title,
    string Description,
    bool Published,
    System.DateTime CreatedAt,
    int QuestionCount);

