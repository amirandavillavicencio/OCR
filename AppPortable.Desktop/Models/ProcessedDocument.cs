namespace AppPortable.Desktop.Models;

public sealed class ProcessedDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SourceFile { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
    public List<DocumentChunk> Chunks { get; set; } = [];
}

public sealed class DocumentChunk
{
    public string ChunkId { get; set; } = Guid.NewGuid().ToString("N");
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
