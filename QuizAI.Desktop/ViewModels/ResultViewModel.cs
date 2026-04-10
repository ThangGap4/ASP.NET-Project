using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class ResultViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private bool _isBusy = true;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private decimal _totalScore;
    [ObservableProperty] private decimal _maxTotalScore;
    [ObservableProperty] private string _scorePercentage = string.Empty;
    [ObservableProperty] private ObservableCollection<AnswerResultViewModel> _answers = new();

    public Guid AttemptId { get; }

    public ResultViewModel(ApiClient api, MainWindowViewModel main, Guid attemptId)
    {
        _api = api;
        _main = main;
        AttemptId = attemptId;
        _ = LoadResultAsync(attemptId);
    }

    private async Task LoadResultAsync(Guid attemptId)
    {
        IsBusy = true;
        StatusMessage = "Loading results...";
        try
        {
            var result = await _api.GetResultAsync(attemptId);
            if (result == null)
            {
                StatusMessage = "Failed to load result";
                return;
            }

            TotalScore = result.TotalScore ?? 0;
            MaxTotalScore = result.MaxTotalScore ?? 0;

            var pct = MaxTotalScore > 0
                ? (double)TotalScore / (double)MaxTotalScore * 100
                : 0;
            ScorePercentage = $"{pct:F1}%";

            Answers = new ObservableCollection<AnswerResultViewModel>(
                result.Answers.Select(a => new AnswerResultViewModel(a, this))
            );
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToLibrary()
    {
        _main.NavigateToLibrary();
    }

    [RelayCommand]
    private void GoToCreateQuiz()
    {
        _main.NavigateToCreateQuiz();
    }

    public async Task ExplainAnswerLocalAsync(AnswerResultViewModel answerVm)
    {
        answerVm.IsBusyExplain = true;
        answerVm.AiExplanation = string.Empty;
        try
        {
            var res = await _api.ExplainAnswerAsync(AttemptId, answerVm.QuestionId);
            if (res != null) {
                answerVm.AiExplanation = $"{res.Explanation}\n\n📝 Trích dẫn: \"{res.ExtractedText}\"";
            }
        }
        catch (Exception ex)
        {
            answerVm.AiExplanation = $"Failed to explain: {ex.Message}";
        }
        finally
        {
            answerVm.IsBusyExplain = false;
        }
    }
}

public partial class AnswerResultViewModel : ObservableObject
{
    public Guid QuestionId { get; }
    public string QuestionPrompt { get; }
    public string QuestionType { get; }
    public string AnswerText { get; }
    public string SelectedOptionContent { get; }
    public string CorrectOptionContent { get; }
    public bool IsCorrect { get; }
    public decimal FinalScore { get; }
    public decimal MaxScore { get; }
    public string FeedbackText { get; }
    public string ScoreColor => IsCorrect ? "#27ae60" : "#e74c3c";
    public string ResultLabel => IsCorrect ? "✓ Correct" : "✗ Incorrect";

    [ObservableProperty] private bool _isBusyExplain;
    [ObservableProperty] private string _aiExplanation = string.Empty;

    private readonly ResultViewModel _parent;

    public AnswerResultViewModel(AnswerResultDto dto, ResultViewModel parent)
    {
        _parent = parent;
        QuestionId = dto.QuestionId;
        QuestionPrompt = dto.QuestionPrompt;
        QuestionType = dto.QuestionType;
        AnswerText = dto.AnswerText ?? string.Empty;
        SelectedOptionContent = dto.SelectedOption?.Content ?? string.Empty;
        CorrectOptionContent = dto.CorrectOption?.Content ?? string.Empty;
        FinalScore = dto.FinalScore;
        MaxScore = dto.MaxScore;

        IsCorrect = dto.QuestionType is "mcq" or "true_false"
            ? dto.SelectedOption?.IsCorrect == true
            : dto.FinalScore >= dto.MaxScore * 0.6m;

        // Parse feedback JSON if present
        if (!string.IsNullOrWhiteSpace(dto.FeedbackJson))
        {
            try
            {
                var fb = JsonSerializer.Deserialize<JsonElement>(dto.FeedbackJson);
                FeedbackText = fb.TryGetProperty("feedback", out var f) ? f.GetString() ?? string.Empty : dto.FeedbackJson;
            }
            catch
            {
                FeedbackText = dto.FeedbackJson;
            }
        }
        else
        {
            FeedbackText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task Explain()
    {
        await _parent.ExplainAnswerLocalAsync(this);
    }
}
