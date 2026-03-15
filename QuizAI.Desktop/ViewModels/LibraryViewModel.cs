using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizAI.Desktop.Services;

namespace QuizAI.Desktop.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly ApiClient _api;
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private ObservableCollection<DocumentDto> _documents = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private string _urlInput = string.Empty;
    [ObservableProperty] private DocumentDto? _selectedDocument;

    [ObservableProperty] private string _pasteText = string.Empty;
    [ObservableProperty] private string _pasteTitle = string.Empty;

    [ObservableProperty] private bool _isEditMode = false;
    [ObservableProperty] private DocumentDto? _editingDocument;
    [ObservableProperty] private string _editFileName = string.Empty;
    [ObservableProperty] private string _editContent = string.Empty;

    public bool IsEmpty => Documents.Count == 0;

    partial void OnDocumentsChanged(ObservableCollection<DocumentDto> value)
    {
        OnPropertyChanged(nameof(IsEmpty));
        value.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    public LibraryViewModel(ApiClient api, MainWindowViewModel main)
    {
        _api = api;
        _main = main;
        _ = LoadDocumentsAsync();
    }

    [RelayCommand]
    private async Task LoadDocumentsAsync()
    {
        IsBusy = true;
        StatusMessage = "Loading...";
        try
        {
            var docs = await _api.GetDocumentsAsync();
            Documents = new ObservableCollection<DocumentDto>(docs);
            StatusMessage = $"{docs.Count} documents";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UploadFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        IsBusy = true;
        StatusMessage = "Uploading...";
        try
        {
            var doc = await _api.UploadDocumentAsync(filePath);
            if (doc != null)
            {
                Documents.Insert(0, doc);
                StatusMessage = $"Uploaded '{doc.FileName}'. Processing...";
                _ = PollProcessingAsync(doc.Id);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UploadUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(UrlInput)) return;

        IsBusy = true;
        StatusMessage = "Importing URL...";
        try
        {
            var doc = await _api.UploadUrlAsync(UrlInput);
            if (doc != null)
            {
                Documents.Insert(0, doc);
                UrlInput = string.Empty;
                StatusMessage = "URL imported. Processing...";
                _ = PollProcessingAsync(doc.Id);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"URL import error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PollProcessingAsync(Guid docId)
    {
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(4000);
            try
            {
                var updated = await _api.GetDocumentAsync(docId);
                if (updated == null) break;

                var idx = Documents.IndexOf(Documents.FirstOrDefault(d => d.Id == docId)!);
                if (idx >= 0) Documents[idx] = updated;

                if (updated.Processed)
                {
                    StatusMessage = $"'{updated.FileName}' is ready.";
                    break;
                }
            }
            catch { break; }
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(Guid id)
    {
        IsBusy = true;
        try
        {
            await _api.DeleteDocumentAsync(id);
            var doc = Documents.FirstOrDefault(d => d.Id == id);
            if (doc != null) Documents.Remove(doc);
            StatusMessage = "Document deleted";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToCreateQuiz()
    {
        _main.NavigateToCreateQuiz();
    }

    [RelayCommand]
    private void Logout()
    {
        _main.Logout();
    }

    [RelayCommand]
    private async Task SavePasteTextAsync()
    {
        if (string.IsNullOrWhiteSpace(PasteText))
        {
            StatusMessage = "Please enter some content.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Saving...";
        try
        {
            var doc = await _api.UploadTextAsync(PasteText, string.IsNullOrWhiteSpace(PasteTitle) ? null : PasteTitle);
            if (doc != null)
            {
                Documents.Insert(0, doc);
                PasteText = string.Empty;
                PasteTitle = string.Empty;
                StatusMessage = $"Saved '{doc.FileName}'. Processing...";
                _ = PollProcessingAsync(doc.Id);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StartEdit(DocumentDto doc)
    {
        EditingDocument = doc;
        EditFileName = doc.FileName;
        EditContent = string.Empty;
        IsEditMode = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        EditingDocument = null;
        EditContent = string.Empty;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (EditingDocument == null) return;

        IsBusy = true;
        try
        {
            await _api.UpdateDocumentAsync(
                EditingDocument.Id,
                string.IsNullOrWhiteSpace(EditFileName) ? null : EditFileName,
                string.IsNullOrWhiteSpace(EditContent) ? null : EditContent
            );

            StatusMessage = "Updated. Re-processing...";
            IsEditMode = false;
            EditingDocument = null;
            await LoadDocumentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
