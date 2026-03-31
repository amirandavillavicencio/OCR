using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using AppPortable.Core.Services;

namespace AppPortable.Tests.Pipeline;

public sealed class PipelineTests
{
    [Fact]
    public async Task ProcessAsync_UsesNativeAndIndexes()
    {
        var pipeline = new DocumentPipelineService(
            new FakePdfService(),
            new FakeOcr(),
            new FakeChunking(),
            new FakeJson(),
            new FakeStorage(),
            new FakeIndex());

        var temp = Path.GetTempFileName();
        await File.WriteAllTextAsync(temp, "fake");

        var result = await pipeline.ProcessAsync(temp);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(ExtractionLayer.Native, result.Pages[0].ExtractionLayer);
    }

    private sealed class FakePdfService : IPdfExtractionService
    {
        public Task<IReadOnlyList<string>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["Texto nativo suficientemente largo para evitar OCR y pasar validación de cincuenta caracteres."]);
    }

    private sealed class FakeOcr : IOcrService
    {
        public bool IsAvailable => true;
        public string AvailabilityMessage => "ok";
        public Task<(string Text, float? Confidence, string? Error)> ExtractTextFromPageAsync(string pdfPath, int pageNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<(string, float?, string?)>(("ocr", 0.9f, null));
    }

    private sealed class FakeChunking : IChunkingService
    {
        public IReadOnlyList<DocumentChunk> Chunk(string documentId, string sourceFile, IReadOnlyList<DocumentPage> pages)
            => [new() { ChunkId = "x", DocumentId = documentId, SourceFile = sourceFile, PageStart = 1, PageEnd = 1, Text = pages[0].Text, ChunkIndex = 0 }];
    }

    private sealed class FakeJson : IJsonPersistenceService
    {
        public Task<IReadOnlyList<ProcessedDocument>> LoadAllProcessedDocumentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProcessedDocument>>([]);
        public Task SaveProcessedDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeStorage : ILocalStorageService
    {
        public string RootPath => Path.GetTempPath();
        public string DocumentsPath => RootPath;
        public string JsonPath => RootPath;
        public string ChunksPath => RootPath;
        public string IndexPath => RootPath;
        public string TempPath => RootPath;
        public string LogsPath => RootPath;
        public string DatabasePath => Path.Combine(RootPath, "t.db");
        public string CopySourcePdf(string sourcePath, string documentId) => sourcePath;
        public void EnsureDirectories() { }
    }

    private sealed class FakeIndex : IIndexService
    {
        public Task IndexAsync(ProcessedDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReindexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
