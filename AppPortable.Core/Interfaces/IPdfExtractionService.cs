namespace AppPortable.Core.Interfaces;

public interface IPdfExtractionService
{
    Task<IReadOnlyList<string>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken = default);
}
