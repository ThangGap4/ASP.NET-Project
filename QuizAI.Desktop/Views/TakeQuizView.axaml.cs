using Avalonia.Controls;
using Avalonia.Interactivity;
using QuizAI.Desktop.ViewModels;

namespace QuizAI.Desktop.Views;

public partial class TakeQuizView : UserControl
{
    public TakeQuizView()
    {
        InitializeComponent();
    }

    private void OnOptionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is TakeQuizViewModel vm)
        {
            if (btn.CommandParameter is OptionViewModel opt)
                vm.SelectOptionCommand.Execute(opt);
        }
    }

    private void OnTrueClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TakeQuizViewModel vm)
            vm.SelectTrueFalse(true);
    }

    private void OnFalseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TakeQuizViewModel vm)
            vm.SelectTrueFalse(false);
    }
}
