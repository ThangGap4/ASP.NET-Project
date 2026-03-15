using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QuizAI.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnImportDocument(object sender, RoutedEventArgs e)
    {
        var status = FindControl<TextBlock>("StatusText");
        if (status != null)
            status.Text = "Chức năng import tài liệu đang phát triển...";
    }

    private void OnCreateQuiz(object sender, RoutedEventArgs e)
    {
        var status = FindControl<TextBlock>("StatusText");
        if (status != null)
            status.Text = "Chức năng tạo quiz đang phát triển...";
    }

    private void OnTakeQuiz(object sender, RoutedEventArgs e)
    {
        var status = FindControl<TextBlock>("StatusText");
        if (status != null)
            status.Text = "Chức năng làm quiz đang phát triển...";
    }
}
