using AppPortable.Core.Interfaces;
using UglyToad.PdfPig;

namespace AppPortable.Infrastructure.PDF;

// PdfPig elegido por licencia Apache 2.0 y API simple por página para extracción local.
public sealed class PdfExtractionService : IPdfExtractionService
{
    public Task<IReadOnlyList<string>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        var pages = new List<string>();
        using var doc = PdfDocument.Open(pdfPath);
        foreach (var page in doc.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(page.Text ?? string.Empty);
        }

        return Task.FromResult<IReadOnlyList<string>>(pages);
    }
}
