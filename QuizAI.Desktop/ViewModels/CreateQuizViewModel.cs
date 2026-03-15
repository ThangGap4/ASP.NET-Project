using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class CreateQuizViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private ObservableCollection<DocumentDto> _documents = new();
    [ObservableProperty] private ObservableCollection<QuizDto> _quizzes = new();
    [ObservableProperty] private DocumentDto? _selectedDocument;
    [ObservableProperty] private int _questionCount = 5;
    [ObservableProperty] private string _selectedDifficulty = "medium";
    [ObservableProperty] private string _quizTitle = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy = false;

    public List<int> QuestionCountOptions { get; } = new() { 5, 10, 15, 20 };
    public List<string> DifficultyOptions { get; } = new() { "easy", "medium", "hard" };

    public CreateQuizViewModel(ApiClient api, MainWindowViewModel main)
    {
        _api = api;
        _main = main;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var docs = await _api.GetDocumentsAsync();
            Documents = new ObservableCollection<DocumentDto>(docs.Where(d => d.Processed));

            var quizzes = await _api.GetQuizzesAsync();
            Quizzes = new ObservableCollection<QuizDto>(quizzes);

            StatusMessage = $"{Documents.Count} documents ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateQuizAsync()
    {
        if (SelectedDocument == null)
        {
            StatusMessage = "Please select a document first";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Generating {QuestionCount} questions... This may take 10-30 seconds";
        try
        {
            var quiz = await _api.GenerateQuizAsync(
                SelectedDocument.Id,
                QuestionCount,
                SelectedDifficulty,
                string.IsNullOrWhiteSpace(QuizTitle) ? null : QuizTitle
            );

            if (quiz != null)
            {
                Quizzes.Insert(0, quiz);
                StatusMessage = $"Quiz '{quiz.Title}' created with {quiz.QuestionCount} questions!";
                QuizTitle = string.Empty;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Generation error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void TakeQuiz(QuizDto quiz)
    {
        _main.NavigateToTakeQuiz(quiz.Id, quiz.Title);
    }

    [RelayCommand]
    private async Task DeleteQuizAsync(Guid id)
    {
        try
        {
            await _api.DeleteQuizAsync(id);
            var quiz = Quizzes.FirstOrDefault(q => q.Id == id);
            if (quiz != null) Quizzes.Remove(quiz);
            StatusMessage = "Quiz deleted";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GoToLibrary()
    {
        _main.NavigateToLibrary();
    }
}
