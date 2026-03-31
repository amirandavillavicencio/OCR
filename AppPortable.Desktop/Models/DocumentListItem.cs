namespace AppPortable.Desktop.Models;

public sealed class DocumentListItem
{
    public string Id { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public string DisplayName => Path.GetFileName(SourceFile);
    public int ChunkCount { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
