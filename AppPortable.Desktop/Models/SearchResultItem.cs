namespace AppPortable.Desktop.Models;

public sealed class SearchResultItem
{
    public string DocumentId { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string FullText { get; set; } = string.Empty;

    public string DisplayTitle => $"{Path.GetFileName(SourceFile)} (p. {PageStart}-{PageEnd})";
}
