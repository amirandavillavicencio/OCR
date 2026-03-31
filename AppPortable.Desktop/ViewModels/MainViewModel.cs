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
    private readonly IDocumentPipelineService _pipelineService;
    private readonly ISearchService _searchService;
    private readonly IIndexService _indexService;

    private string _searchText = string.Empty;
    private string _statusMessage = "Listo.";
    private bool _isBusy;
    private double _progressValue;
    private SearchResultItem? _selectedSearchResult;

    public MainViewModel(IDocumentPipelineService pipelineService, ISearchService searchService, IIndexService indexService)
    {
        _pipelineService = pipelineService;
        _searchService = searchService;
        _indexService = indexService;

        LoadDocumentCommand = new AsyncRelayCommand(LoadDocumentAsync, () => !IsBusy);
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => !IsBusy);
        ReindexCommand = new AsyncRelayCommand(ReindexAsync, () => !IsBusy);

        _ = LoadInitialDataAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProcessedDocument> Documents { get; } = [];
    public ObservableCollection<SearchResultItem> SearchResults { get; } = [];

    public ICommand LoadDocumentCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ReindexCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public SearchResultItem? SelectedSearchResult
    {
        get => _selectedSearchResult;
        set => SetProperty(ref _selectedSearchResult, value);
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            var documents = await _pipelineService.LoadProcessedDocumentsAsync();
            foreach (var document in documents)
            {
                Documents.Add(document);
            }
            StatusMessage = $"Documentos cargados: {Documents.Count}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar documentos iniciales: {ex.Message}";
        }
    }

    private async Task LoadDocumentAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Multiselect = false,
            Title = "Seleccionar documento PDF"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;
        ProgressValue = 0;
        StatusMessage = "Procesando documento...";

        try
        {
            var progress = new Progress<double>(value => ProgressValue = Math.Clamp(value * 100, 0, 100));
            var result = await _pipelineService.ProcessPdfAsync(dialog.FileName, progress);
            await _indexService.IndexChunksAsync(result.Chunks);

            Documents.Insert(0, result.Document);
            StatusMessage = $"Documento procesado: {result.Document.DocumentName} ({result.Document.ChunkCount} chunks).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error en procesamiento: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            ProgressValue = 0;
        }
    }

    private async Task SearchAsync()
    {
        IsBusy = true;
        StatusMessage = "Buscando...";

        try
        {
            var results = await _searchService.SearchAsync(SearchText.Trim());
            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            SelectedSearchResult = SearchResults.FirstOrDefault();
            StatusMessage = $"Resultados: {SearchResults.Count}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error en búsqueda: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReindexAsync()
    {
        IsBusy = true;
        StatusMessage = "Reconstruyendo índice...";

        try
        {
            var chunks = await _pipelineService.LoadAllChunksAsync();
            await _indexService.RebuildIndexAsync(chunks);
            StatusMessage = $"Índice reconstruido con {chunks.Count} chunks.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al reindexar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void RaiseCommandCanExecuteChanged()
    {
        (LoadDocumentCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (SearchCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ReindexCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }
}
