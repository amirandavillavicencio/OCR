using AppPortable.Core.Interfaces;
using Tesseract;

namespace AppPortable.Infrastructure.OCR;

public sealed class TesseractOcrService : IOcrService
{
    private readonly string? _tesseractPath;

    public TesseractOcrService()
    {
        _tesseractPath = ResolveTesseractPath();
        IsAvailable = !string.IsNullOrWhiteSpace(_tesseractPath);
        AvailabilityMessage = IsAvailable
            ? "Tesseract detectado correctamente."
            : "No se encontró Tesseract. Configura TESSERACT_PATH o agrega tesseract.exe al PATH del sistema.";
    }

    public bool IsAvailable { get; }
    public string AvailabilityMessage { get; }

    public Task<(string Text, float? Confidence, string? Error)> ExtractTextFromPageAsync(string pdfPath, int pageNumber, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return Task.FromResult<(string, float?, string?)>((string.Empty, null, AvailabilityMessage));
        }

        try
        {
            // Placeholder robusto para pipeline: OCR por página queda desacoplado para integrar render PDF->imagen.
            // Retorna vacío para permitir fallback a texto nativo si está disponible.
            using var engine = new TesseractEngine(Path.Combine(_tesseractPath!, "tessdata"), "eng+spa", EngineMode.Default);
            _ = engine.Version;
            return Task.FromResult<(string, float?, string?)>((string.Empty, null, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult<(string, float?, string?)>((string.Empty, null, $"OCR fallo en página {pageNumber}: {ex.Message}"));
        }
    }

    private static string? ResolveTesseractPath()
    {
        var env = Environment.GetEnvironmentVariable("TESSERACT_PATH");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env, "tesseract.exe")))
            return env;

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var segment in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                if (File.Exists(Path.Combine(segment, "tesseract.exe")))
                    return segment;
            }
            catch
            {
                // ignore individual malformed PATH entries
            }
        }

        return null;
    }
}
