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
    
    [ObservableProperty] private ObservableCollection<QuestionStatisticsDto> _questions = new();

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
            Questions = new ObservableCollection<QuestionStatisticsDto>(data.Questions);
            
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
