using AppPortable.Desktop.Models;
using iText.Kernel.Pdf;

namespace AppPortable.Desktop.Services;

public sealed class DocumentPipelineService
{
    public Task<ProcessedDocument> ProcessPdfAsync(
        string pdfPath,
        Action<double, string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            onProgress?.Invoke(5, "Leyendo PDF...");
            using var reader = new PdfReader(pdfPath);
            using var pdf = new PdfDocument(reader);

            var pageCount = pdf.GetNumberOfPages();
            var chunks = new List<DocumentChunk>(pageCount);

            for (var i = 1; i <= pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = pdf.GetPage(i);
                var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var normalized = text.Trim();
                chunks.Add(new DocumentChunk
                {
                    PageStart = i,
                    PageEnd = i,
                    Snippet = normalized.Length <= 180 ? normalized : normalized[..180] + "...",
                    Text = normalized
                });

                var progress = 5 + (85d * i / Math.Max(pageCount, 1));
                onProgress?.Invoke(progress, $"Procesando página {i}/{pageCount}...");
            }

            var document = new ProcessedDocument
            {
                SourceFile = pdfPath,
                ProcessedAtUtc = DateTime.UtcNow,
                Chunks = chunks
            };

            onProgress?.Invoke(95, "Finalizando...");
            return document;
        }, cancellationToken);
    }
}
