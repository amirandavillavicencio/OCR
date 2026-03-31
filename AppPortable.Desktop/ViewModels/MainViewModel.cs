using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AppPortable.Desktop.Commands;
using AppPortable.Desktop.Models;
using AppPortable.Desktop.Services;
using Microsoft.Win32;

namespace AppPortable.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly DocumentPipelineService _pipelineService;
    private readonly DocumentStoreService _documentStore;
    private readonly ISearchService _searchService;

    private DocumentListItem? _selectedDocument;
    private SearchResultItem? _selectedSearchResult;
    private string _searchText = string.Empty;
    private string _statusMessage = "Listo";
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private double _progressValue;

    public MainViewModel()
    {
        _pipelineService = new DocumentPipelineService();
        _documentStore = new DocumentStoreService();
        _searchService = new SqliteFtsSearchService();

        LoadDocumentCommand = new RelayCommand(async () => await LoadDocumentAsync(), () => !IsBusy);
        SearchCommand = new RelayCommand(async () => await SearchAsync(), () => !IsBusy);
        ReindexCommand = new RelayCommand(async () => await ReindexAsync(), () => !IsBusy);

        _ = RefreshDocumentsAsync();
    }

    public ObservableCollection<DocumentListItem> Documents { get; } = [];

    public ObservableCollection<SearchResultItem> Results { get; } = [];

    public DocumentListItem? SelectedDocument
    {
        get => _selectedDocument;
        set => SetProperty(ref _selectedDocument, value);
    }

    public SearchResultItem? SelectedSearchResult
    {
        get => _selectedSearchResult;
        set => SetProperty(ref _selectedSearchResult, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                (LoadDocumentCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ReindexCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public ICommand LoadDocumentCommand { get; }

    public ICommand SearchCommand { get; }

    public ICommand ReindexCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task LoadDocumentAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            var dialog = new OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Title = "Selecciona un PDF"
            };

            if (dialog.ShowDialog() != true)
            {
                StatusMessage = "Carga cancelada";
                return;
            }

            IsBusy = true;
            ProgressValue = 0;
            StatusMessage = "Iniciando carga...";

            var document = await _pipelineService.ProcessPdfAsync(
                dialog.FileName,
                (progress, message) =>
                {
                    ProgressValue = progress;
                    StatusMessage = message;
                });

            await _documentStore.SaveAsync(document);
            ProgressValue = 96;
            StatusMessage = "Indexando...";
            await _searchService.IndexDocumentAsync(document);
            ProgressValue = 100;

            await RefreshDocumentsAsync();
            StatusMessage = $"Documento cargado: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Error al cargar el documento";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            ProgressValue = 15;
            StatusMessage = "Buscando...";

            var items = await _searchService.SearchAsync(SearchText);
            Results.Clear();
            foreach (var item in items)
            {
                Results.Add(item);
            }

            ProgressValue = 100;
            StatusMessage = $"Resultados: {Results.Count}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Error durante búsqueda";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReindexAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            ProgressValue = 0;
            StatusMessage = "Reindexando...";

            var docs = await _documentStore.LoadAllAsync();
            ProgressValue = 45;
            await _searchService.RebuildIndexAsync(docs);
            ProgressValue = 100;
            await RefreshDocumentsAsync();

            StatusMessage = $"Reindexación completa: {docs.Count} documentos";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Error durante reindexación";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshDocumentsAsync()
    {
        var docs = await _documentStore.LoadAllAsync();
        Documents.Clear();
        foreach (var d in docs)
        {
            Documents.Add(new DocumentListItem
            {
                Id = d.Id,
                SourceFile = d.SourceFile,
                ProcessedAtUtc = d.ProcessedAtUtc,
                ChunkCount = d.Chunks.Count
            });
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
