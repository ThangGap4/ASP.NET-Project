using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class TakeQuizViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    private Guid _attemptId;
    private List<QuestionDto> _questions = new();

    [ObservableProperty] private int _currentQuestionIndex = 0;
    [ObservableProperty] private QuestionDto? _currentQuestion;
    [ObservableProperty] private string _quizTitle = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private bool _isLoaded = false;
    [ObservableProperty] private string _progress = string.Empty;

    // Stores answers: QuestionId → (selectedOptionId, answerText)
    private readonly Dictionary<Guid, (Guid? OptionId, string? Text)> _answers = new();

    // Current question answer state
    [ObservableProperty] private ObservableCollection<OptionViewModel> _currentOptions = new();
    [ObservableProperty] private string _essayAnswer = string.Empty;

    public bool HasPrevious => CurrentQuestionIndex > 0;
    public bool HasNext => CurrentQuestionIndex < _questions.Count - 1;
    public bool IsLastQuestion => CurrentQuestionIndex == _questions.Count - 1;
    public bool IsEssayQuestion => CurrentQuestion?.Type is "short_answer" or "long_answer" or "essay";
    public bool IsMcqQuestion => CurrentQuestion?.Type is "mcq" or null;
    public bool IsTrueFalseQuestion => CurrentQuestion?.Type == "true_false";
    public bool IsFillBlankQuestion => CurrentQuestion?.Type == "fill_blank";

    public TakeQuizViewModel(ApiClient api, MainWindowViewModel main, Guid quizId, string quizTitle)
    {
        _api = api;
        _main = main;
        QuizTitle = quizTitle;
        _ = StartQuizAsync(quizId);
    }

    private async Task StartQuizAsync(Guid quizId)
    {
        IsBusy = true;
        StatusMessage = "Starting quiz...";
        try
        {
            var attempt = await _api.StartAttemptAsync(quizId);
            if (attempt == null)
            {
                StatusMessage = "Failed to start quiz";
                return;
            }
            _attemptId = attempt.Id;

            var detail = await _api.GetQuizAsync(quizId);
            if (detail == null)
            {
                StatusMessage = "Failed to load questions";
                return;
            }

            _questions = detail.Questions;
            IsLoaded = true;
            LoadQuestion(0);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message.Contains("not published")
                ? "This quiz is not published yet"
                : $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadQuestion(int index)
    {
        if (index < 0 || index >= _questions.Count) return;

        // Save current answer before navigating
        SaveCurrentAnswer();

        CurrentQuestionIndex = index;
        CurrentQuestion = _questions[index];
        Progress = $"Question {index + 1} / {_questions.Count}";

        // Restore previous answer if exists
        CurrentOptions = new ObservableCollection<OptionViewModel>(
            CurrentQuestion.Options.Select(o => new OptionViewModel(o))
        );

        if (_answers.TryGetValue(CurrentQuestion.Id, out var saved))
        {
            if (saved.OptionId.HasValue)
            {
                var opt = CurrentOptions.FirstOrDefault(o => o.Id == saved.OptionId.Value);
                if (opt != null) opt.IsSelected = true;
            }
            EssayAnswer = saved.Text ?? string.Empty;
        }
        else
        {
            EssayAnswer = string.Empty;
        }

        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(IsLastQuestion));
        OnPropertyChanged(nameof(IsEssayQuestion));
        OnPropertyChanged(nameof(IsMcqQuestion));
        OnPropertyChanged(nameof(IsTrueFalseQuestion));
        OnPropertyChanged(nameof(IsFillBlankQuestion));
    }

    private void SaveCurrentAnswer()
    {
        if (CurrentQuestion == null) return;

        var selectedOpt = CurrentOptions.FirstOrDefault(o => o.IsSelected);
        _answers[CurrentQuestion.Id] = (selectedOpt?.Id, EssayAnswer);
    }

    [RelayCommand]
    private void SelectOption(OptionViewModel opt)
    {
        foreach (var o in CurrentOptions) o.IsSelected = false;
        opt.IsSelected = true;
    }

    public void SelectTrueFalse(bool value)
    {
        EssayAnswer = value ? "True" : "False";
        var match = CurrentOptions.FirstOrDefault(o =>
            o.Content.Equals(value ? "True" : "False", StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            foreach (var o in CurrentOptions) o.IsSelected = false;
            match.IsSelected = true;
        }
    }

    [RelayCommand]
    private void NextQuestion()
    {
        LoadQuestion(CurrentQuestionIndex + 1);
    }

    [RelayCommand]
    private void PreviousQuestion()
    {
        LoadQuestion(CurrentQuestionIndex - 1);
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        SaveCurrentAnswer();

        IsBusy = true;
        StatusMessage = "Grading your answers... Please wait";
        try
        {
            var answers = _answers.Select(kv => new AnswerItemDto(
                kv.Key,
                kv.Value.OptionId,
                kv.Value.Text
            )).ToList();

            var result = await _api.SubmitAttemptAsync(_attemptId, answers);
            if (result != null)
                _main.NavigateToResult(_attemptId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Submit error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _main.NavigateToCreateQuiz();
    }
}

public partial class OptionViewModel : ObservableObject
{
    public Guid Id { get; }
    public int OptIndex { get; }
    public string Content { get; }

    [ObservableProperty]
    private bool _isSelected;

    public OptionViewModel(OptionDto dto)
    {
        Id = dto.Id;
        OptIndex = dto.OptIndex;
        Content = dto.Content;
    }
}
