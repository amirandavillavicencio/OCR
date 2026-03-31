namespace AppPortable.Desktop.Models;

public sealed class DocumentChunkStore
{
    public required ProcessedDocument Document { get; init; }
    public required IReadOnlyList<DocumentChunk> Chunks { get; init; }
}
