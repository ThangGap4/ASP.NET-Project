using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _role = string.Empty;
    [ObservableProperty] private string _lastLogin = string.Empty;
    [ObservableProperty] private int _totalDocuments;
    [ObservableProperty] private int _totalQuizzes;
    [ObservableProperty] private int _totalAttempts;
    [ObservableProperty] private double _averageScorePercent;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy = false;

    public string AverageScoreText => $"{AverageScorePercent:F1}%";

    public string[] AvailableLanguages { get; } = { "en-US", "vi-VN" };

    private string _selectedLanguage = App.CurrentLanguage;
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                App.SetLanguage(value);
            }
        }
    }

    public ProfileViewModel(ApiClient api, MainWindowViewModel main)
    {
        _api = api;
        _main = main;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await _api.GetMeAsync();
            if (profile == null) return;

            DisplayName = profile.DisplayName;
            Email = profile.Email;
            Role = profile.Role;
            LastLogin = profile.LastLogin.HasValue
                ? profile.LastLogin.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : "Never";
            TotalDocuments = profile.TotalDocuments;
            TotalQuizzes = profile.TotalQuizzes;
            TotalAttempts = profile.TotalAttempts;
            AverageScorePercent = profile.AverageScorePercent;
            OnPropertyChanged(nameof(AverageScoreText));
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
        _main.NavigateToLibrary();
    }
}
