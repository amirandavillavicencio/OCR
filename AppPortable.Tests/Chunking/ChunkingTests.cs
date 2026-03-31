using AppPortable.Core.Enums;
using AppPortable.Core.Models;
using AppPortable.Infrastructure.Chunking;

namespace AppPortable.Tests.Chunking;

public sealed class ChunkingTests
{
    [Fact]
    public void EmptyText_ReturnsNoChunks()
    {
        var service = new ChunkingService();
        var chunks = service.Chunk("doc", "file", [new DocumentPage { PageNumber = 1, ExtractionLayer = ExtractionLayer.Native, Text = "" }]);
        Assert.Empty(chunks);
    }

    [Fact]
    public void SinglePage_LongText_ReturnsChunks()
    {
        var text = string.Join("\n\n", Enumerable.Repeat("Este es un párrafo válido con más de cien caracteres para asegurar que entre en chunking correctamente y sea estable.", 40));
        var service = new ChunkingService();
        var chunks = service.Chunk("doc", "file", [new DocumentPage { PageNumber = 1, ExtractionLayer = ExtractionLayer.Native, Text = text }]);
        Assert.NotEmpty(chunks);
    }
}
