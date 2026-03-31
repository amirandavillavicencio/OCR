using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AppPortable.Core;
using AppPortable.Desktop.Commands;
using AppPortable.Infrastructure;
using AppPortable.Search;
using Microsoft.Win32;

namespace AppPortable.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IDocumentPipelineService _documentPipelineService;
    private readonly ISearchService _searchService;
    private DocumentListItem? _selectedDocument;
    private SearchResult? _selectedSearchResult;
    private string _searchText = string.Empty;
    private string _statusMessage = "Listo";
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private int _progressValue;

    public MainViewModel(IDocumentPipelineService documentPipelineService, ISearchService searchService)
    {
        _documentPipelineService = documentPipelineService;
        _searchService = searchService;

        Documents = new ObservableCollection<DocumentListItem>();
        Results = new ObservableCollection<SearchResult>();

        LoadDocumentCommand = new RelayCommand(_ => LoadDocumentAsync(), _ => !IsBusy);
        SearchCommand = new RelayCommand(_ => SearchAsync(), _ => !IsBusy);
        ReindexCommand = new RelayCommand(_ => ReindexAsync(), _ => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DocumentListItem> Documents { get; }
    public ObservableCollection<SearchResult> Results { get; }

    public DocumentListItem? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            _selectedDocument = value;
            OnPropertyChanged();
        }
    }

    public SearchResult? SelectedSearchResult
    {
        get => _selectedSearchResult;
        set
        {
            _selectedSearchResult = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedResultDetails));
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            RaiseCommandCanExecuteChanged();
        }
    }

    public int ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged();
        }
    }

    public string SelectedResultDetails
    {
        get
        {
            if (SelectedSearchResult is null)
            {
                return "Selecciona un resultado para ver detalle.";
            }

            return $"Archivo: {SelectedSearchResult.SourceFile}\n" +
                   $"Páginas: {SelectedSearchResult.PageStart} - {SelectedSearchResult.PageEnd}\n\n" +
                   $"Snippet:\n{SelectedSearchResult.Snippet}\n\n" +
                   $"Texto completo:\n{SelectedSearchResult.ChunkText}";
        }
    }

    public ICommand LoadDocumentCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ReindexCommand { get; }

    public async Task InitializeAsync()
    {
        await _searchService.InitializeAsync();
        await RefreshDocumentsAsync();
    }

    private async Task LoadDocumentAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            var dialog = new OpenFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                Multiselect = false,
                Title = "Selecciona un PDF"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            IsBusy = true;
            ProgressValue = 0;
            StatusMessage = "Procesando PDF...";

            var progress = new Progress<int>(value => ProgressValue = value);
            var processed = await _documentPipelineService.ProcessPdfAsync(dialog.FileName, progress);

            StatusMessage = "Indexando contenido...";
            ProgressValue = 85;
            await _searchService.IndexDocumentAsync(processed);

            await RefreshDocumentsAsync();

            ProgressValue = 100;
            StatusMessage = "Documento cargado e indexado correctamente.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Error al cargar documento.";
        }
        finally
        {
            IsBusy = false;
            if (ProgressValue < 100)
            {
                ProgressValue = 0;
            }
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            StatusMessage = "Buscando...";
            ProgressValue = 30;

            var found = await _searchService.SearchAsync(SearchText);
            Results.Clear();
            foreach (var item in found)
            {
                Results.Add(item);
            }

            StatusMessage = $"Resultados: {Results.Count}";
            ProgressValue = 100;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Error durante búsqueda.";
        }
        finally
        {
            IsBusy = false;
            if (ProgressValue < 100)
            {
                ProgressValue = 0;
            }
        }
    }

    private async Task ReindexAsync()
    {
        try
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            ProgressValue = 10;
            StatusMessage = "Reconstruyendo índice...";

            var docs = await _documentPipelineService.LoadAllDocumentsAsync();
            ProgressValue = 40;
            await _searchService.RebuildIndexAsync(docs);
            ProgressValue = 100;
            StatusMessage = "Índice reconstruido.";

            await RefreshDocumentsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Error al reindexar.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshDocumentsAsync()
    {
        var docs = await _documentPipelineService.LoadAllDocumentsAsync();
        Documents.Clear();
        foreach (var doc in docs)
        {
            Documents.Add(new DocumentListItem(doc.Id, doc.FileName, doc.SourceFile, doc.ProcessedAtUtc, doc.Chunks.Count));
        }
    }

    private void RaiseCommandCanExecuteChanged()
    {
        if (LoadDocumentCommand is RelayCommand load)
        {
            load.RaiseCanExecuteChanged();
        }

        if (SearchCommand is RelayCommand search)
        {
            search.RaiseCanExecuteChanged();
        }

        if (ReindexCommand is RelayCommand reindex)
        {
            reindex.RaiseCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record DocumentListItem(string Id, string Name, string SourcePath, DateTime ProcessedAtUtc, int ChunkCount)
{
    public string Display => $"{Name} ({ChunkCount} chunks)";
}
