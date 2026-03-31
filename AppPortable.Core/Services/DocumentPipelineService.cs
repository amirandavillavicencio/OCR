using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Core.Services;

public sealed class DocumentPipelineService(
    IPdfExtractionService pdfExtractionService,
    IOcrService ocrService,
    IChunkingService chunkingService,
    IJsonPersistenceService jsonPersistenceService,
    ILocalStorageService localStorageService,
    IIndexService indexService) : IDocumentProcessor
{
    public async Task<ProcessedDocument> ProcessAsync(string sourcePdfPath, CancellationToken cancellationToken = default)
    {
        localStorageService.EnsureDirectories();
        var pagesText = await pdfExtractionService.ExtractPagesAsync(sourcePdfPath, cancellationToken);
        var documentId = BuildDocumentId(sourcePdfPath);
        var copiedPath = localStorageService.CopySourcePdf(sourcePdfPath, documentId);

        var pages = new List<DocumentPage>();
        for (var i = 0; i < pagesText.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nativeText = pagesText[i]?.Trim() ?? string.Empty;
            var invalidRatio = GetInvalidCharRatio(nativeText);

            if (nativeText.Length >= 50 && invalidRatio <= 0.30)
            {
                pages.Add(new DocumentPage
                {
                    PageNumber = i + 1,
                    ExtractionLayer = ExtractionLayer.Native,
                    Text = nativeText
                });
                continue;
            }

            if (!ocrService.IsAvailable)
            {
                pages.Add(new DocumentPage
                {
                    PageNumber = i + 1,
                    ExtractionLayer = nativeText.Length > 0 ? ExtractionLayer.Mixed : ExtractionLayer.Failed,
                    Text = nativeText,
                    Warnings = ["OCR no disponible: " + ocrService.AvailabilityMessage]
                });
                continue;
            }

            var (ocrText, confidence, error) = await ocrService.ExtractTextFromPageAsync(sourcePdfPath, i + 1, cancellationToken);
            var finalText = string.IsNullOrWhiteSpace(ocrText) ? nativeText : ocrText.Trim();
            var layer = string.IsNullOrWhiteSpace(nativeText) ? ExtractionLayer.Ocr : ExtractionLayer.Mixed;
            var warnings = new List<string>();
            if (!string.IsNullOrWhiteSpace(error)) warnings.Add(error);
            if (string.IsNullOrWhiteSpace(finalText)) layer = ExtractionLayer.Failed;

            pages.Add(new DocumentPage
            {
                PageNumber = i + 1,
                ExtractionLayer = layer,
                OcrConfidence = confidence,
                Text = finalText,
                Warnings = warnings
            });
        }

        var processed = new ProcessedDocument
        {
            DocumentId = documentId,
            SourceFile = copiedPath,
            ProcessedAt = DateTimeOffset.UtcNow,
            TotalPages = pages.Count,
            Pages = pages,
            ExtractionSummary = new ExtractionSummary
            {
                Native = pages.Count(p => p.ExtractionLayer == ExtractionLayer.Native),
                Ocr = pages.Count(p => p.ExtractionLayer == ExtractionLayer.Ocr || p.ExtractionLayer == ExtractionLayer.Mixed),
                Failed = pages.Count(p => p.ExtractionLayer == ExtractionLayer.Failed)
            }
        };

        processed.Chunks = chunkingService.Chunk(processed.DocumentId, processed.SourceFile, processed.Pages).ToList();
        processed.Warnings.AddRange(pages.SelectMany(p => p.Warnings));

        await jsonPersistenceService.SaveProcessedDocumentAsync(processed, cancellationToken);
        await indexService.IndexAsync(processed, cancellationToken);
        return processed;
    }

    private static double GetInvalidCharRatio(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        var invalid = Regex.Matches(text, "[^\\p{L}\\p{N}\\p{P}\\p{Zs}\\r\\n]").Count;
        return (double)invalid / text.Length;
    }

    private static string BuildDocumentId(string sourcePdfPath)
    {
        var input = $"{sourcePdfPath}|{File.GetLastWriteTimeUtc(sourcePdfPath):O}|{new FileInfo(sourcePdfPath).Length}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
