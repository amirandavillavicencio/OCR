using System.Text.Json;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Infrastructure.Persistence;

public sealed class JsonPersistenceService(ILocalStorageService localStorageService) : IJsonPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task SaveProcessedDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
    {
        localStorageService.EnsureDirectories();
        var docPath = Path.Combine(localStorageService.JsonPath, $"{document.DocumentId}.processed.json");
        var chunksPath = Path.Combine(localStorageService.ChunksPath, $"{document.DocumentId}.chunks.json");

        await File.WriteAllTextAsync(docPath, JsonSerializer.Serialize(document, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(chunksPath, JsonSerializer.Serialize(document.Chunks, JsonOptions), cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessedDocument>> LoadAllProcessedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        localStorageService.EnsureDirectories();
        var files = Directory.GetFiles(localStorageService.JsonPath, "*.processed.json");
        var docs = new List<ProcessedDocument>();
        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var doc = JsonSerializer.Deserialize<ProcessedDocument>(json, JsonOptions);
            if (doc is not null) docs.Add(doc);
        }

        return docs.OrderByDescending(x => x.ProcessedAt).ToList();
    }
}
