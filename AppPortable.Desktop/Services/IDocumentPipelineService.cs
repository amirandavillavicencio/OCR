using AppPortable.Desktop.Models;

namespace AppPortable.Desktop.Services;

public interface IDocumentPipelineService
{
    Task<DocumentChunkStore> ProcessPdfAsync(string pdfPath, IProgress<double>? progress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessedDocument>> LoadProcessedDocumentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> LoadAllChunksAsync(CancellationToken cancellationToken = default);
}
