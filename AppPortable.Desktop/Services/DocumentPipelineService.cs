using System.Text.Json;
using System.Text.RegularExpressions;
using AppPortable.Desktop.Models;
using UglyToad.PdfPig;

namespace AppPortable.Desktop.Services;

public sealed class DocumentPipelineService : IDocumentPipelineService
{
    private readonly string _dataRoot;
    private readonly string _documentsDirectory;

    public DocumentPipelineService(string? dataRoot = null)
    {
        _dataRoot = dataRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppPortable");
        _documentsDirectory = Path.Combine(_dataRoot, "documents");
        Directory.CreateDirectory(_documentsDirectory);
    }

    public async Task<DocumentChunkStore> ProcessPdfAsync(string pdfPath, IProgress<double>? progress, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException("No se encontró el PDF seleccionado.", pdfPath);
        }

        progress?.Report(0.05);
        var documentName = Path.GetFileNameWithoutExtension(pdfPath);
        var documentId = $"{documentName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var normalized = ExtractTextFromPdf(pdfPath);
        progress?.Report(0.40);

        var chunks = ChunkText(documentId, documentName, normalized, 500);
        progress?.Report(0.70);

        var processed = new ProcessedDocument
        {
            DocumentId = documentId,
            DocumentName = documentName,
            SourcePath = pdfPath,
            OutputJsonPath = Path.Combine(_documentsDirectory, $"{documentId}.json"),
            ChunkCount = chunks.Count
        };

        var payload = new DocumentChunkStore { Document = processed, Chunks = chunks };

        await using var stream = File.Create(processed.OutputJsonPath);
        await JsonSerializer.SerializeAsync(stream, payload, cancellationToken: cancellationToken);

        progress?.Report(1.0);
        return payload;
    }

    public async Task<IReadOnlyList<ProcessedDocument>> LoadProcessedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var stores = await LoadAllStoresAsync(cancellationToken);
        return stores.Select(s => s.Document)
            .OrderByDescending(d => d.DocumentId)
            .ToList();
    }

    public async Task<IReadOnlyList<DocumentChunk>> LoadAllChunksAsync(CancellationToken cancellationToken = default)
    {
        var stores = await LoadAllStoresAsync(cancellationToken);
        return stores.SelectMany(s => s.Chunks).ToList();
    }

    private async Task<IReadOnlyList<DocumentChunkStore>> LoadAllStoresAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_documentsDirectory, "*.json", SearchOption.TopDirectoryOnly);
        var result = new List<DocumentChunkStore>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(file);
            var store = await JsonSerializer.DeserializeAsync<DocumentChunkStore>(stream, cancellationToken: cancellationToken);
            if (store is not null)
            {
                result.Add(store);
            }
        }

        return result;
    }

    private static string ExtractTextFromPdf(string pdfPath)
    {
        using var document = PdfDocument.Open(pdfPath);
        var pages = document.GetPages().Select(page => page.Text).Where(text => !string.IsNullOrWhiteSpace(text));
        var fullText = string.Join(Environment.NewLine, pages);
        var cleaned = Regex.Replace(fullText, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Documento sin texto extraíble." : cleaned;
    }

    private static List<DocumentChunk> ChunkText(string documentId, string documentName, string content, int chunkSize)
    {
        var chunks = new List<DocumentChunk>();
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var buffer = new List<string>();
        var currentLength = 0;
        var chunkNumber = 1;

        foreach (var word in words)
        {
            if (currentLength + word.Length + 1 > chunkSize && buffer.Count > 0)
            {
                chunks.Add(CreateChunk(documentId, documentName, chunkNumber++, buffer));
                buffer.Clear();
                currentLength = 0;
            }

            buffer.Add(word);
            currentLength += word.Length + 1;
        }

        if (buffer.Count > 0)
        {
            chunks.Add(CreateChunk(documentId, documentName, chunkNumber, buffer));
        }

        if (chunks.Count == 0)
        {
            chunks.Add(new DocumentChunk
            {
                ChunkId = $"{documentId}-1",
                DocumentId = documentId,
                DocumentName = documentName,
                ChunkNumber = 1,
                Content = "Documento sin contenido."
            });
        }

        return chunks;
    }

    private static DocumentChunk CreateChunk(string documentId, string documentName, int chunkNumber, IEnumerable<string> words)
    {
        return new DocumentChunk
        {
            ChunkId = $"{documentId}-{chunkNumber}",
            DocumentId = documentId,
            DocumentName = documentName,
            ChunkNumber = chunkNumber,
            Content = string.Join(' ', words)
        };
    }
}
