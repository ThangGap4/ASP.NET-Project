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
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Server is waking up, please try again in a moment...";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
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

        if (Password.Length < 6)
        {
            ErrorMessage = "Password must be at least 6 characters";
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
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "Server is waking up, please try again in a moment...";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
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
