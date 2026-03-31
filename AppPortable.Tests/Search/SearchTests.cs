using AppPortable.Core.Models;
using AppPortable.Search.Indexing;
using AppPortable.Search.Search;

namespace AppPortable.Tests.Search;

public sealed class SearchTests
{
    [Fact]
    public async Task IndexAndSearch_ReturnsResults()
    {
        var db = Path.Combine(Path.GetTempPath(), $"appportable-test-{Guid.NewGuid():N}.db");
        var index = new FtsIndexService(db);
        var search = new FtsSearchService(db);

        var doc = new ProcessedDocument
        {
            DocumentId = "doc1",
            SourceFile = "c:/tmp/a.pdf",
            Chunks =
            [
                new DocumentChunk { ChunkId = "c1", DocumentId = "doc1", SourceFile = "a.pdf", PageStart = 1, PageEnd = 1, Text = "contrato de arrendamiento y cláusulas de pago" }
            ]
        };

        await index.IndexAsync(doc);
        var results = await search.SearchAsync("arrendamiento");
        Assert.NotEmpty(results);
    }
}
