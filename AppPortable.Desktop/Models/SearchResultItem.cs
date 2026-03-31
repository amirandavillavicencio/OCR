namespace AppPortable.Desktop.Models;

public sealed class SearchResultItem
{
    public required string ChunkId { get; init; }
    public required string DocumentName { get; init; }
    public required int ChunkNumber { get; init; }
    public required string Content { get; init; }
    public required double Score { get; init; }
}
