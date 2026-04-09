using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class QuizParticipantsViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;
    
    [ObservableProperty] private Guid _quizId;
    [ObservableProperty] private string _quizTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<QuizParticipantDto> _participants = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy = false;

    public QuizParticipantsViewModel(ApiClient api, MainWindowViewModel main, Guid quizId, string quizTitle)
    {
        _api = api;
        _main = main;
        QuizId = quizId;
        QuizTitle = quizTitle;
        _ = LoadParticipantsAsync();
    }

    [RelayCommand]
    private async Task LoadParticipantsAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading results...";
        try
        {
            var data = await _api.GetQuizResultsAsync(QuizId);
            Participants = new ObservableCollection<QuizParticipantDto>(data);
            if (data.Count == 0)
                StatusMessage = "No participants have taken this quiz yet.";
            else
                StatusMessage = $"{data.Count} attempts found.";
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
    private void GoBack()
    {
        _main.CurrentView = new CreateQuizViewModel(_api, _main); // or Library, typically CreateQuiz
    }
}