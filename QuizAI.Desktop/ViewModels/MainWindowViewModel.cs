using CommunityToolkit.Mvvm.ComponentModel;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private string _currentUserName = string.Empty;

    [ObservableProperty]
    private bool _isLoggedIn = false;

    public MainWindowViewModel(ApiClient api)
    {
        _api = api;
        // Start at Login view
        NavigateToLogin();
    }

    public void NavigateToLogin()
    {
        CurrentView = new LoginViewModel(_api, this);
        IsLoggedIn = false;
        CurrentUserName = string.Empty;
    }

    public void NavigateToLibrary()
    {
        CurrentView = new LibraryViewModel(_api, this);
    }

    public void NavigateToCreateQuiz()
    {
        CurrentView = new CreateQuizViewModel(_api, this);
    }

    public void NavigateToTakeQuiz(Guid quizId, string quizTitle)
    {
        CurrentView = new TakeQuizViewModel(_api, this, quizId, quizTitle);
    }

    public void NavigateToResult(Guid attemptId)
    {
        CurrentView = new ResultViewModel(_api, this, attemptId);
    }

    public void OnLoggedIn(string displayName)
    {
        IsLoggedIn = true;
        CurrentUserName = displayName;
        NavigateToLibrary();
    }

    public void Logout()
    {
        _api.ClearToken();
        NavigateToLogin();
    }
}
