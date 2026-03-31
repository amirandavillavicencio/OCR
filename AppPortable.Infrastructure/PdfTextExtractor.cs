using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace AppPortable.Infrastructure;

public static class PdfTextExtractor
{
    public static string ExtractAllText(string pdfPath)
    {
        using var reader = new PdfReader(pdfPath);
        using var pdf = new PdfDocument(reader);

        var pageCount = pdf.GetNumberOfPages();
        var parts = new List<string>(pageCount);

        for (var i = 1; i <= pageCount; i++)
        {
            var page = pdf.GetPage(i);
            var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page);
            if (!string.IsNullOrWhiteSpace(text))
            {
                parts.Add(text);
            }
        }

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }
}
