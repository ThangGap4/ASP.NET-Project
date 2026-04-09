using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuizAI.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private string? _token;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient()
    {
        var baseUrl = Environment.GetEnvironmentVariable("QUIZAI_API_URL")
             //?? "https://asp-net-project-9dm5.onrender.com/api/";
             ?? "http://localhost:5127/api/";

        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public void SetToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _token = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    // ─── AUTH ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto?> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("auth/login", new { email, password });
        if ((int)res.StatusCode == 401)
            throw new UnauthorizedAccessException("Invalid email or password");
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
    }

    public async Task<AuthResponseDto?> RegisterAsync(string email, string password, string displayName)
    {
        var res = await _http.PostAsJsonAsync("auth/register", new { email, password, displayName });
        if ((int)res.StatusCode == 409)
            throw new InvalidOperationException("Email already registered");
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
    }

    // ─── DOCUMENTS ───────────────────────────────────────────────────────────

    public async Task<List<DocumentDto>> GetDocumentsAsync()
    {
        var res = await _http.GetFromJsonAsync<List<DocumentDto>>("documents", JsonOptions);
        return res ?? new();
    }

    public async Task<DocumentDto?> UploadDocumentAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        var fileName = Path.GetFileName(filePath);
        var content = new ByteArrayContent(fileBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
        form.Add(content, "file", fileName);

        var res = await _http.PostAsync("documents/upload", form);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
    }

    public async Task<DocumentDto?> UploadUrlAsync(string url, string? title = null)
    {
        var res = await _http.PostAsJsonAsync("documents/upload-url", new { url, title });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
    }

    public async Task<DocumentDto?> UploadTextAsync(string content, string? title = null)
    {
        var res = await _http.PostAsJsonAsync("documents/upload-text", new { content, title });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
    }

    public async Task UpdateDocumentAsync(Guid id, string? fileName, string? content)
    {
        var res = await _http.PatchAsJsonAsync($"documents/{id}", new { fileName, content });
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteDocumentAsync(Guid id)
    {
        var res = await _http.DeleteAsync($"documents/{id}");
        res.EnsureSuccessStatusCode();
    }

    public async Task<DocumentDto?> GetDocumentAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<DocumentDto>($"documents/{id}", JsonOptions);
    }

    // ─── QUIZZES ─────────────────────────────────────────────────────────────

    public async Task<List<QuizDto>> GetQuizzesAsync()
    {
        var res = await _http.GetFromJsonAsync<List<QuizDto>>("quizzes", JsonOptions);
        return res ?? new();
    }

    public async Task<QuizDetailDto?> GetQuizAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<QuizDetailDto>($"quizzes/{id}", JsonOptions);
    }

    public async Task<QuizDto?> GenerateQuizAsync(Guid documentId, int questionCount, string difficulty, string questionType = "mcq", string? title = null)
    {
        var res = await _http.PostAsJsonAsync("quizzes/generate", new
        {
            documentId,
            questionCount,
            difficulty,
            questionType,
            title
        });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<QuizDto>(JsonOptions);
    }

    public async Task DeleteQuizAsync(Guid id)
    {
        var res = await _http.DeleteAsync($"quizzes/{id}");
        res.EnsureSuccessStatusCode();
    }

    public async Task<bool> PublishQuizAsync(Guid id, bool published)
    {
        var res = await _http.PatchAsJsonAsync($"quizzes/{id}/publish", new { published });
        res.EnsureSuccessStatusCode();
        return published;
    }

    // ─── ATTEMPTS ────────────────────────────────────────────────────────────

    public async Task<AttemptStartDto?> StartAttemptAsync(Guid quizId)
    {
        var res = await _http.PostAsJsonAsync("attempts/start", new { quizId });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<AttemptStartDto>(JsonOptions);
    }

    public async Task<AttemptSummaryDto?> SubmitAttemptAsync(Guid attemptId, List<AnswerItemDto> answers)
    {
        var res = await _http.PostAsJsonAsync($"attempts/{attemptId}/submit", new { answers });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<AttemptSummaryDto>(JsonOptions);
    }

    public async Task<AttemptResultDto?> GetResultAsync(Guid attemptId)
    {
        return await _http.GetFromJsonAsync<AttemptResultDto>($"attempts/{attemptId}/result", JsonOptions);
    }

    public async Task<List<AttemptSummaryDto>> GetMyAttemptsAsync()
    {
        var res = await _http.GetFromJsonAsync<List<AttemptSummaryDto>>("attempts/my", JsonOptions);
        return res ?? new();
    }

    public async Task<List<QuizParticipantDto>> GetQuizResultsAsync(Guid quizId)
    {
        var res = await _http.GetFromJsonAsync<List<QuizParticipantDto>>($"attempts/quiz/{quizId}", JsonOptions);
        return res ?? new();
    }

    public async Task<UserProfileDto?> GetMeAsync()
    {
        return await _http.GetFromJsonAsync<UserProfileDto>("auth/me", JsonOptions);
    }

    // ─── ADMIN ───────────────────────────────────────────────────────────────

    public async Task<SystemStatsDto?> GetSystemStatsAsync()
    {
        return await _http.GetFromJsonAsync<SystemStatsDto>("admin/stats", JsonOptions);
    }

    public async Task<List<AdminDocumentDto>> GetAdminDocumentsAsync()
    {
        var res = await _http.GetFromJsonAsync<List<AdminDocumentDto>>("admin/documents", JsonOptions);
        return res ?? new();
    }

    public async Task AdminDeleteDocumentAsync(Guid docId)
    {
        var res = await _http.DeleteAsync($"admin/documents/{docId}");
        res.EnsureSuccessStatusCode();
    }

    public async Task<List<AdminUserDto>> GetAdminUsersAsync()
    {
        var res = await _http.GetFromJsonAsync<List<AdminUserDto>>("admin/users", JsonOptions);
        return res ?? new();
    }

    public async Task<List<AdminUserHistoryDto>> GetUserHistoryAsync(Guid userId)
    {
        var res = await _http.GetFromJsonAsync<List<AdminUserHistoryDto>>($"admin/users/{userId}/history", JsonOptions);
        return res ?? new();
    }

    public async Task<List<AdminQuizDto>> GetAdminQuizzesAsync()
    {
        var res = await _http.GetFromJsonAsync<List<AdminQuizDto>>("admin/quizzes", JsonOptions);
        return res ?? new();
    }

    public async Task<bool> ToggleUserBanAsync(Guid userId)
    {
        var res = await _http.PostAsync($"admin/users/{userId}/toggle-ban", null);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return data.GetProperty("isBanned").GetBoolean();
    }

    public async Task ForceUnpublishQuizAsync(Guid quizId)
    {
        var res = await _http.PatchAsync($"admin/quizzes/{quizId}/unpublish", null);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteAdminQuizAsync(Guid quizId)
    {
        var res = await _http.DeleteAsync($"admin/quizzes/{quizId}");
        res.EnsureSuccessStatusCode();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record AuthResponseDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    string Token
);

public record DocumentDto(
    Guid Id,
    string FileName,
    string? MimeType,
    long FileSize,
    bool Processed,
    DateTime UploadedAt,
    int ChunkCount
);

public record QuizDto(
    Guid Id,
    string Title,
    string? Description,
    bool Published,
    DateTime CreatedAt,
    Guid? SourceDocumentId,
    int QuestionCount,
    bool IsOwner
);

public record QuizDetailDto(
    Guid Id,
    string Title,
    string? Description,
    bool Published,
    DateTime CreatedAt,
    List<QuestionDto> Questions
);

public record QuestionDto(
    Guid Id,
    int Seq,
    string Type,
    string Prompt,
    decimal MaxScore,
    List<OptionDto> Options
);

public record OptionDto(Guid Id, int OptIndex, string Content);

public record AttemptStartDto(
    Guid Id,
    Guid QuizId,
    DateTime StartedAt,
    string Status,
    decimal MaxTotalScore
);

public record AnswerItemDto(
    Guid QuestionId,
    Guid? SelectedOptionId,
    string? AnswerText
);

public record AttemptSummaryDto(
    Guid Id,
    Guid QuizId,
    string? QuizTitle,
    decimal? TotalScore,
    decimal? MaxTotalScore,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt
);

public record AttemptResultDto(
    Guid Id,
    Guid QuizId,
    decimal? TotalScore,
    decimal? MaxTotalScore,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    List<AnswerResultDto> Answers
);

public record AnswerResultDto(
    Guid Id,
    string QuestionPrompt,
    string QuestionType,
    string? AnswerText,
    OptionResultDto? SelectedOption,
    OptionResultDto? CorrectOption,
    decimal? AutoScore,
    decimal FinalScore,
    decimal MaxScore,
    string? FeedbackJson
);

public record OptionResultDto(string Content, bool IsCorrect);

public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime? LastLogin,
    int TotalDocuments,
    int TotalQuizzes,
    int TotalAttempts,
    double AverageScorePercent
);

// ─── ADMIN DTOs ──────────────────────────────────────────────────────────────

public record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsBanned,
    DateTime CreatedAt,
    DateTime? LastLogin,
    int QuizCount,
    int AttemptCount
);

public record TopUserDto(Guid Id, string DisplayName, int AttemptCount, double AverageScore);
public record TopQuizDto(Guid Id, string Title, int AttemptCount);

public record SystemStatsDto(
    int TotalUsers,
    int TotalQuizzes,
    int TotalPublishedQuizzes,
    int TotalAttempts,
    List<TopUserDto>? TopUsers = null,
    List<TopQuizDto>? TopQuizzes = null
);

public record AdminUserHistoryDto(
    Guid Id,
    Guid QuizId,
    string QuizTitle,
    int Score,
    int TotalQuestions,
    DateTime CompletedAt
);

public record AdminDocumentDto(
    Guid Id,
    string FileName,
    DateTime UploadedAt,
    bool Processed,
    string OwnerName,
    long Tokens,
    string FileType
);

public record AdminQuizDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime CreatedAt,
    bool Published,
    Guid? CreatorId,
    string CreatorName,
    int QuestionCount,
    int AttemptCount
);

public record QuizParticipantDto(
    Guid Id,
    Guid UserId,
    string UserName,
    Guid QuizId,
    string QuizTitle,
    decimal? TotalScore,
    decimal? MaxTotalScore,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt
);

