using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using QuizAI.Desktop.ViewModels;

namespace QuizAI.Desktop.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }

    private async void OnPickFile(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select a document",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Documents")
                    {
                        Patterns = new[] { "*.txt", "*.pdf", "*.docx" }
                    }
                }
            });

        if (files.Count > 0 && DataContext is LibraryViewModel vm)
        {
            var localPath = files[0].Path.LocalPath;
            if (!string.IsNullOrEmpty(localPath))
                await vm.UploadFileCommand.ExecuteAsync(localPath);
        }
    }

    private void OnDeleteDocument(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is LibraryViewModel vm)
        {
            var doc = btn.DataContext as QuizAI.Desktop.Services.DocumentDto;
            if (doc != null)
                vm.DeleteDocumentCommand.Execute(doc.Id);
        }
    }

    private void OnEditDocument(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is LibraryViewModel vm)
        {
            var doc = btn.DataContext as QuizAI.Desktop.Services.DocumentDto;
            if (doc != null)
                vm.StartEditCommand.Execute(doc);
        }
    }
}
