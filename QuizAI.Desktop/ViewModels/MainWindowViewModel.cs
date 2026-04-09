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

    [ObservableProperty]
    private bool _isAdmin = false;

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
        IsAdmin = false;
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

    public void NavigateToParticipants(Guid quizId, string quizTitle)
    {
        CurrentView = new QuizParticipantsViewModel(_api, this, quizId, quizTitle);
    }

    public void NavigateToResult(Guid attemptId)
    {
        CurrentView = new ResultViewModel(_api, this, attemptId);
    }

    public void NavigateToHistory()
    {
        CurrentView = new HistoryViewModel(_api, this);
    }

    public void NavigateToProfile()
    {
        CurrentView = new ProfileViewModel(_api, this);
    }

    public void NavigateToAdmin()
    {
        CurrentView = new AdminDashboardViewModel(_api, this);
    }

    public void OnLoggedIn(string displayName, string role)
    {
        IsLoggedIn = true;
        IsAdmin = role == "Admin";
        CurrentUserName = displayName;
        NavigateToLibrary();
    }

    public void Logout()
    {
        _api.ClearToken();
        NavigateToLogin();
    }
}
