using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private bool _isRegisterMode = false;

    public LoginViewModel(ApiClient api, MainWindowViewModel main)
    {
        _api = api;
        _main = main;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter email and password";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.LoginAsync(Email, Password);
            if (result != null)
            {
                _api.SetToken(result.Token);
                _main.OnLoggedIn(result.DisplayName);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("401") || ex.Message.Contains("Unauthorized")
                ? "Invalid email or password"
                : $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Please fill in all fields";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _api.RegisterAsync(Email, Password, DisplayName);
            if (result != null)
            {
                _api.SetToken(result.Token);
                _main.OnLoggedIn(result.DisplayName);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("409") || ex.Message.Contains("Conflict")
                ? "Email already registered"
                : $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = string.Empty;
    }
}
