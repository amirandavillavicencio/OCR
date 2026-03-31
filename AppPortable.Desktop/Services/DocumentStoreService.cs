using System.Text.Json;
using AppPortable.Desktop.Models;

namespace AppPortable.Desktop.Services;

public sealed class DocumentStoreService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string DataDirectory { get; }

    public DocumentStoreService()
    {
        DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppPortable", "data");
        Directory.CreateDirectory(DataDirectory);
    }

    public async Task SaveAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
    {
        var path = GetDocumentPath(document.Id);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    public async Task<List<ProcessedDocument>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<ProcessedDocument>();
        foreach (var file in Directory.EnumerateFiles(DataDirectory, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var doc = await JsonSerializer.DeserializeAsync<ProcessedDocument>(stream, cancellationToken: cancellationToken);
            if (doc is not null)
            {
                list.Add(doc);
            }
        }

        return list.OrderByDescending(d => d.ProcessedAtUtc).ToList();
    }

    public string GetDocumentPath(string documentId) => Path.Combine(DataDirectory, $"{documentId}.json");
}
