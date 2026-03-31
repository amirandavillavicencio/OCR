using AppPortable.Core.Enums;

namespace AppPortable.Core.Models;

public sealed class DocumentChunk
{
    public string ChunkId { get; set; } = Guid.NewGuid().ToString("N");
    public string DocumentId { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public int ChunkIndex { get; set; }
    public string Text { get; set; } = string.Empty;
    public int TextLength => Text.Length;
    public List<ExtractionLayer> ExtractionLayersInvolved { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = [];
}
