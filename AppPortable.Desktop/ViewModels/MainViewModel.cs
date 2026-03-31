using System.Collections.ObjectModel;
using System.Windows;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using AppPortable.Core.Services;
using AppPortable.Desktop.Commands;
using AppPortable.Infrastructure.Chunking;
using AppPortable.Infrastructure.OCR;
using AppPortable.Infrastructure.PDF;
using AppPortable.Infrastructure.Persistence;
using AppPortable.Infrastructure.Storage;
using AppPortable.Search.Indexing;
using AppPortable.Search.Search;
using Microsoft.Win32;

namespace AppPortable.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly ILocalStorageService _storage;
    private readonly IJsonPersistenceService _json;
    private readonly IIndexService _index;
    private readonly IDocumentProcessor _processor;
    private readonly ISearchService _search;
    private string _query = string.Empty;
    private string _status = "Listo";
    private bool _isBusy;
    private int _progress;
    private object? _selectedDetail;

    public MainViewModel()
    {
        _storage = new LocalStorageService();
        _json = new JsonPersistenceService(_storage);
        _index = new FtsIndexService(_storage.DatabasePath);
        var ocr = new TesseractOcrService();
        _processor = new DocumentPipelineService(new PdfExtractionService(), ocr, new ChunkingService(), _json, _storage, _index);
        _search = new FtsSearchService(_storage.DatabasePath);

        LoadDocumentCommand = new AsyncRelayCommand(LoadDocumentAsync, () => !IsBusy);
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => !IsBusy);
        ReindexCommand = new AsyncRelayCommand(ReindexAsync, () => !IsBusy);

        OcrStatus = ocr.AvailabilityMessage;
        _ = LoadExistingAsync();
    }

    public ObservableCollection<ProcessedDocument> Documents { get; } = [];
    public ObservableCollection<SearchResult> Results { get; } = [];

    public AsyncRelayCommand LoadDocumentCommand { get; }
    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand ReindexCommand { get; }

    public string OcrStatus { get; }

    public string Query
    {
        get => _query;
        set => Set(ref _query, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (Set(ref _isBusy, value))
            {
                LoadDocumentCommand.RaiseCanExecuteChanged();
                SearchCommand.RaiseCanExecuteChanged();
                ReindexCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int Progress
    {
        get => _progress;
        set => Set(ref _progress, value);
    }

    public object? SelectedDetail
    {
        get => _selectedDetail;
        set => Set(ref _selectedDetail, value);
    }

    private async Task LoadExistingAsync()
    {
        var docs = await _json.LoadAllProcessedDocumentsAsync();
        foreach (var doc in docs)
            Documents.Add(doc);
    }

    private async Task LoadDocumentAsync()
    {
        var dialog = new OpenFileDialog { Filter = "PDF Files (*.pdf)|*.pdf" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            IsBusy = true;
            Progress = 20;
            Status = "Procesando documento...";
            var doc = await _processor.ProcessAsync(dialog.FileName);
            Progress = 90;
            Documents.Insert(0, doc);
            Status = $"Documento procesado: {doc.DocumentId}";
            SelectedDetail = doc;
            Progress = 100;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error procesando documento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Status = "Error durante procesamiento.";
        }
        finally
        {
            await Task.Delay(200);
            Progress = 0;
            IsBusy = false;
        }
    }

    private async Task SearchAsync()
    {
        try
        {
            IsBusy = true;
            Results.Clear();
            var results = await _search.SearchAsync(Query);
            foreach (var result in results)
                Results.Add(result);
            Status = $"{results.Count} resultados";
        }
        catch (Exception ex)
        {
            Status = "Error en búsqueda.";
            MessageBox.Show($"Error de búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            IsBusy = true;
            var all = await _json.LoadAllProcessedDocumentsAsync();
            await _index.ReindexAsync(all);
            Status = "Índice reconstruido correctamente.";
        }
        catch (Exception ex)
        {
            Status = "Error reindexando.";
            MessageBox.Show($"Error reindexando: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
