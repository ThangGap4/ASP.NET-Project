using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class QuizStatisticsViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private Guid _quizId;
    [ObservableProperty] private string _quizTitle = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    
    [ObservableProperty] private int _totalParticipants;
    [ObservableProperty] private string _averageScoreText = string.Empty;
    
    [ObservableProperty] private ObservableCollection<QuestionStatisticsViewModel> _questions = new();

    public QuizStatisticsViewModel(ApiClient api, MainWindowViewModel main, Guid quizId, string quizTitle)
    {
        _api = api;
        _main = main;
        QuizId = quizId;
        QuizTitle = quizTitle;
        _ = LoadStatisticsAsync();
    }

    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        IsBusy = true;
        StatusMessage = "Đang tải dữ liệu biểu đồ...";
        try
        {
            var data = await _api.GetQuizStatisticsAsync(QuizId);
            if (data == null)
            {
                StatusMessage = "Không thể tải thống kê.";
                return;
            }

            TotalParticipants = data.TotalParticipants;
            AverageScoreText = $"{data.AverageScorePercent:F1}%";
            
            var questionVms = data.Questions.Select(q => new QuestionStatisticsViewModel(q, _api, QuizId));
            Questions = new ObservableCollection<QuestionStatisticsViewModel>(questionVms);
            
            if (TotalParticipants == 0)
                StatusMessage = "Chưa có ai tham gia làm bài này.";
            else
                StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _main.CurrentView = new QuizParticipantsViewModel(_api, _main, QuizId, QuizTitle);
    }
}

public partial class QuestionStatisticsViewModel : ObservableObject
{
    public QuestionStatisticsDto Data { get; }
    private readonly ApiClient _api;
    private readonly Guid _quizId;

    [ObservableProperty] private bool _isBusyExplain;
    [ObservableProperty] private string _aiExplanation = string.Empty;

    public QuestionStatisticsViewModel(QuestionStatisticsDto data, ApiClient api, Guid quizId)
    {
        Data = data;
        _api = api;
        _quizId = quizId;
    }

    [RelayCommand]
    private async Task ExplainAsync()
    {
        if (IsBusyExplain) return;
        IsBusyExplain = true;
        try
        {
            var res = await _api.ExplainQuestionAsync(_quizId, Data.QuestionId);
            if (res != null)
                AiExplanation = $"{res.Explanation}\n\n*Trích xuất: {res.ExtractedText}*";
        }
        catch (Exception ex)
        {
            AiExplanation = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsBusyExplain = false;
        }
    }
}
