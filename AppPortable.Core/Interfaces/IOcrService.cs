namespace AppPortable.Core.Interfaces;

public interface IOcrService
{
    bool IsAvailable { get; }
    string AvailabilityMessage { get; }
    Task<(string Text, float? Confidence, string? Error)> ExtractTextFromPageAsync(string pdfPath, int pageNumber, CancellationToken cancellationToken = default);
}
