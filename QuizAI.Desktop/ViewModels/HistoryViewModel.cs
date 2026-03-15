using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private ObservableCollection<AttemptSummaryDto> _attempts = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy = false;

    public HistoryViewModel(ApiClient api, MainWindowViewModel main)
    {
        _api = api;
        _main = main;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var list = await _api.GetMyAttemptsAsync();
            Attempts = new ObservableCollection<AttemptSummaryDto>(
                list.OrderByDescending(a => a.StartedAt));
            StatusMessage = list.Count == 0 ? "No attempts yet." : $"{list.Count} attempt(s)";
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
    private void ViewResult(AttemptSummaryDto attempt)
    {
        _main.NavigateToResult(attempt.Id);
    }

    [RelayCommand]
    private void GoBack()
    {
        _main.NavigateToLibrary();
    }
}
