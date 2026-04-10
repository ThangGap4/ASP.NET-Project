using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using System;

namespace QuizAI.Desktop;

public partial class App : Application
{
    public static string CurrentLanguage { get; private set; } = "en-US";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static void SetLanguage(string langCode)
    {
        CurrentLanguage = langCode;
        var dictionary = new ResourceInclude(new Uri("avares://QuizAI.Desktop/App.axaml"))
        {
            Source = new Uri($"avares://QuizAI.Desktop/Assets/Lang/{langCode}.axaml")
        };

        var appDict = Current.Resources as ResourceDictionary;
        if (appDict != null)
        {
            appDict.MergedDictionaries.Clear();
            appDict.MergedDictionaries.Add(dictionary);
        }
    }

    public static string GetString(string key)
    {
        if (Current.Resources.TryGetResource(key, null, out var val) && val is string s)
            return s;
        return key; // fallback
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
