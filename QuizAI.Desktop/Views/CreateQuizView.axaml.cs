using Avalonia.Controls;
using Avalonia.Interactivity;
using QuizAI.Desktop.Services;
using QuizAI.Desktop.ViewModels;

namespace QuizAI.Desktop.Views;

public partial class CreateQuizView : UserControl
{
    public CreateQuizView()
    {
        InitializeComponent();
    }

    private void OnTakeQuiz(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is CreateQuizViewModel vm)
        {
            if (btn.CommandParameter is QuizDto quiz)
                vm.TakeQuizCommand.Execute(quiz);
        }
    }

    private void OnViewResults(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is CreateQuizViewModel vm)
        {
            if (btn.CommandParameter is QuizDto quiz)
                vm.ViewResultsCommand.Execute(quiz);
        }
    }

    private void OnDeleteQuiz(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is CreateQuizViewModel vm)
        {
            if (btn.CommandParameter is Guid id)
                vm.DeleteQuizCommand.Execute(id);
        }
    }

    private void OnTogglePublish(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is CreateQuizViewModel vm)
        {
            if (btn.CommandParameter is QuizDto quiz)
                vm.TogglePublishCommand.Execute(quiz);
        }
    }

    private void OnCopyQuizId(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Guid id)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            _ = clipboard?.SetTextAsync(id.ToString());
        }
    }
}
