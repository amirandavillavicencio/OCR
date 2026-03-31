namespace AppPortable.Desktop.Models;

public sealed class DocumentChunk
{
    public required string ChunkId { get; init; }
    public required string DocumentId { get; init; }
    public required string DocumentName { get; init; }
    public required int ChunkNumber { get; init; }
    public required string Content { get; init; }
}
