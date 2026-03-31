namespace AppPortable.Core;

public sealed class ProcessedDocument
{
    public required string Id { get; init; }
    public required string SourceFile { get; init; }
    public required string FileName { get; init; }
    public DateTime ProcessedAtUtc { get; init; }
    public List<DocumentChunk> Chunks { get; init; } = new();
}

public sealed class DocumentChunk
{
    public required string ChunkId { get; init; }
    public required string DocumentId { get; init; }
    public required string Text { get; init; }
    public required string Snippet { get; init; }
    public int PageStart { get; init; }
    public int PageEnd { get; init; }
}

public sealed class SearchResult
{
    public required string ChunkId { get; init; }
    public required string DocumentId { get; init; }
    public required string SourceFile { get; init; }
    public required string Snippet { get; init; }
    public required string ChunkText { get; init; }
    public int PageStart { get; init; }
    public int PageEnd { get; init; }
}
