using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace QuizAI.Desktop.ViewModels;

public partial class AdminDashboardViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private SystemStatsDto? _stats;

    [ObservableProperty] private AdminUserDto? _selectedUser;
    
    public ObservableCollection<AdminUserHistoryDto> SelectedUserHistory { get; } = new();

    public ObservableCollection<AdminUserDto> Users { get; } = new();
    public ObservableCollection<AdminQuizDto> PublishedQuizzes { get; } = new();
    public ObservableCollection<AdminDocumentDto> Documents { get; } = new();

    partial void OnSelectedUserChanged(AdminUserDto? value)
    {
        if (value != null)
        {
            _ = LoadUserHistoryAsync(value.Id);
        }
        else
        {
            SelectedUserHistory.Clear();
        }
    }

    private async Task LoadUserHistoryAsync(Guid userId)
    {
        try
        {
            var history = await _api.GetUserHistoryAsync(userId);
            SelectedUserHistory.Clear();
            foreach (var h in history) SelectedUserHistory.Add(h);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading user history: {ex.Message}";
        }
    }

    public AdminDashboardViewModel(ApiClient api, MainWindowViewModel main)
    {
        _api = api;
        _main = main;
        LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    private void GoBack()
    {
        _main.NavigateToLibrary(); // Hoặc bạn có thể tạo một NavigateToHome/Dashboard
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            Stats = await _api.GetSystemStatsAsync();

            var users = await _api.GetAdminUsersAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(u);

            var quizzes = await _api.GetAdminQuizzesAsync();
            PublishedQuizzes.Clear();
            foreach (var q in quizzes) PublishedQuizzes.Add(q);

            var docs = await _api.GetAdminDocumentsAsync();
            Documents.Clear();
            foreach (var d in docs) Documents.Add(d);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading admin data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleBanAsync(Guid userId)
    {
        try
        {
            await _api.ToggleUserBanAsync(userId);
            await LoadDataAsync(); // Reload list
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error banning user: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PreviewQuiz(AdminQuizDto quiz)
    {
        _main.NavigateToTakeQuiz(quiz.Id, $"[PREVIEW] {quiz.Title}");
    }

    [RelayCommand]
    private async Task ForceUnpublishAsync(Guid quizId)
    {
        try
        {
            await _api.ForceUnpublishQuizAsync(quizId);
            await LoadDataAsync(); // Reload list
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error unpublishing quiz: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteQuizAsync(Guid quizId)
    {
        try
        {
            await _api.DeleteAdminQuizAsync(quizId);
            await LoadDataAsync(); // Reload list
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting quiz: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(Guid docId)
    {
        try
        {
            await _api.AdminDeleteDocumentAsync(docId);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting document: {ex.Message}";
        }
    }
}