using Avalonia.Controls;
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
}

