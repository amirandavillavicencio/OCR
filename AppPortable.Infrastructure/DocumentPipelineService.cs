using System.Text.Json;
using AppPortable.Core;

namespace AppPortable.Infrastructure;

public interface IDocumentPipelineService
{
    Task<ProcessedDocument> ProcessPdfAsync(string pdfPath, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessedDocument>> LoadAllDocumentsAsync(CancellationToken cancellationToken = default);
}

public sealed class DocumentPipelineService : IDocumentPipelineService
{
    private readonly string _documentsDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public DocumentPipelineService(string appDataDirectory)
    {
        _documentsDirectory = Path.Combine(appDataDirectory, "documents");
        Directory.CreateDirectory(_documentsDirectory);
    }

    public async Task<ProcessedDocument> ProcessPdfAsync(string pdfPath, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            throw new FileNotFoundException("No se encontró el PDF seleccionado.", pdfPath);
        }

        progress?.Report(10);
        var allText = await Task.Run(() => PdfTextExtractor.ExtractAllText(pdfPath), cancellationToken);

        progress?.Report(50);
        var documentId = Guid.NewGuid().ToString("N");
        var chunks = BuildChunks(allText, documentId);

        var processedDocument = new ProcessedDocument
        {
            Id = documentId,
            SourceFile = pdfPath,
            FileName = Path.GetFileName(pdfPath),
            ProcessedAtUtc = DateTime.UtcNow,
            Chunks = chunks
        };

        var destinationFile = Path.Combine(_documentsDirectory, $"{processedDocument.Id}.json");
        await using var fileStream = File.Create(destinationFile);
        await JsonSerializer.SerializeAsync(fileStream, processedDocument, _jsonOptions, cancellationToken);

        progress?.Report(100);
        return processedDocument;
    }

    public async Task<IReadOnlyList<ProcessedDocument>> LoadAllDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ProcessedDocument>();
        if (!Directory.Exists(_documentsDirectory))
        {
            return result;
        }

        foreach (var file in Directory.EnumerateFiles(_documentsDirectory, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var document = await JsonSerializer.DeserializeAsync<ProcessedDocument>(stream, cancellationToken: cancellationToken);
            if (document is not null)
            {
                result.Add(document);
            }
        }

        return result
            .OrderByDescending(d => d.ProcessedAtUtc)
            .ToList();
    }

    private static List<DocumentChunk> BuildChunks(string allText, string documentId)
    {
        const int chunkSize = 1200;
        const int overlap = 150;
        var cleaned = (allText ?? string.Empty).Replace("\r", " ").Replace("\n", " ");

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return
            [
                new DocumentChunk
                {
                    ChunkId = Guid.NewGuid().ToString("N"),
                    DocumentId = documentId,
                    Text = string.Empty,
                    Snippet = "(Documento sin texto extraíble)",
                    PageStart = 1,
                    PageEnd = 1
                }
            ];
        }

        var chunks = new List<DocumentChunk>();
        var start = 0;
        while (start < cleaned.Length)
        {
            var length = Math.Min(chunkSize, cleaned.Length - start);
            var text = cleaned.Substring(start, length).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                chunks.Add(new DocumentChunk
                {
                    ChunkId = Guid.NewGuid().ToString("N"),
                    DocumentId = documentId,
                    Text = text,
                    Snippet = text.Length > 180 ? $"{text[..180]}..." : text,
                    PageStart = 1,
                    PageEnd = 1
                });
            }

            if (start + length >= cleaned.Length)
            {
                break;
            }

            start += Math.Max(1, chunkSize - overlap);
        }

        return chunks;
    }
}
