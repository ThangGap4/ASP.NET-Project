using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using QuizAI.Desktop.ViewModels;

namespace QuizAI.Desktop.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void OnToggleMode(object? sender, RoutedEventArgs e)
    {
        // Toggle button text manually since BoolConverters.ToObject not available
        if (DataContext is LoginViewModel vm)
        {
            var btn = sender as Button;
            if (btn != null)
                btn.Content = vm.IsRegisterMode ? "Already have account? Login" : "No account? Register";
        }
    }
}
