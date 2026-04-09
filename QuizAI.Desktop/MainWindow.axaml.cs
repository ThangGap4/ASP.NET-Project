using Avalonia.Controls;
using Avalonia.Interactivity;
using QuizAI.Desktop.Services;
using QuizAI.Desktop.ViewModels;

namespace QuizAI.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var api = new ApiClient();
        DataContext = new MainWindowViewModel(api);
    }

    private void OnHistoryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.NavigateToHistory();
    }

    private void OnProfileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.NavigateToProfile();
    }

    private void OnAdminClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.NavigateToAdmin();
    }
}

