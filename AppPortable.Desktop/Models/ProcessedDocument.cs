namespace AppPortable.Desktop.Models;

public sealed class ProcessedDocument
{
    public required string DocumentId { get; init; }
    public required string DocumentName { get; init; }
    public required string SourcePath { get; init; }
    public required string OutputJsonPath { get; init; }
    public required int ChunkCount { get; init; }
}
