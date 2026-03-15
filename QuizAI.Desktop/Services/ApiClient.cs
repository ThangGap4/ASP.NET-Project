using System.Net.Http.Json;

namespace QuizAI.Desktop.Services;

public class ApiClient
{
    private readonly string _baseUrl = "http://localhost:5127/api";

    public async Task<List<QuizDto>> GetQuizzesAsync()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{_baseUrl}/quizzes");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsAsync<List<QuizDto>>() ?? [];
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error fetching quizzes: {ex.Message}");
        }
        return [];
    }

    public async Task<QuizDto> GetQuizAsync(Guid id)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{_baseUrl}/quizzes/{id}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsAsync<QuizDto>();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error fetching quiz: {ex.Message}");
        }
        return null;
    }

    public async Task<Guid> CreateQuizAsync(string title, string description)
    {
        try
        {
            using var client = new HttpClient();
            var dto = new { title, description, creatorId = Guid.NewGuid() };
            var response = await client.PostAsJsonAsync($"{_baseUrl}/quizzes", dto);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<dynamic>();
                return Guid.Parse(result.id.ToString());
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error creating quiz: {ex.Message}");
        }
        return Guid.Empty;
    }

    public async Task<bool> DeleteQuizAsync(Guid id)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.DeleteAsync($"{_baseUrl}/quizzes/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error deleting quiz: {ex.Message}");
        }
        return false;
    }
}

public record QuizDto(
    Guid Id,
    string Title,
    string Description,
    bool Published,
    DateTime CreatedAt,
    int QuestionCount);
