using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Infrastructure.Chunking;

public sealed class ChunkingService(ChunkingOptions? options = null) : IChunkingService
{
    private readonly ChunkingOptions _options = options ?? new ChunkingOptions();

    public IReadOnlyList<DocumentChunk> Chunk(string documentId, string sourceFile, IReadOnlyList<DocumentPage> pages)
    {
        var paragraphs = pages
            .SelectMany(p => SplitParagraphs(p.Text).Select(text => (p.PageNumber, p.ExtractionLayer, Text: text.Trim())))
            .Where(p => p.Text.Length > 0)
            .ToList();

        var chunks = new List<DocumentChunk>();
        var current = new List<(int Page, ExtractionLayer Layer, string Text)>();
        var currentLength = 0;
        var index = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentLength + paragraph.Text.Length > _options.MaxCharacters && current.Count > 0)
            {
                AddChunk(chunks, current, documentId, sourceFile, index++);
                var overlapTarget = (int)(_options.TargetCharacters * _options.OverlapRatio);
                current = BuildOverlap(current, overlapTarget);
                currentLength = current.Sum(x => x.Text.Length);
            }

            current.Add(paragraph);
            currentLength += paragraph.Text.Length;

            if (currentLength >= _options.TargetCharacters)
            {
                AddChunk(chunks, current, documentId, sourceFile, index++);
                var overlapTarget = (int)(_options.TargetCharacters * _options.OverlapRatio);
                current = BuildOverlap(current, overlapTarget);
                currentLength = current.Sum(x => x.Text.Length);
            }
        }

        if (current.Count > 0)
        {
            AddChunk(chunks, current, documentId, sourceFile, index);
        }

        return chunks.Where(c => c.TextLength >= _options.MinChunkLength).ToList();
    }

    private static IEnumerable<string> SplitParagraphs(string text)
        => text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);

    private static List<(int Page, ExtractionLayer Layer, string Text)> BuildOverlap(
        IReadOnlyList<(int Page, ExtractionLayer Layer, string Text)> source,
        int target)
    {
        var overlap = new List<(int, ExtractionLayer, string)>();
        var total = 0;
        for (var i = source.Count - 1; i >= 0; i--)
        {
            overlap.Insert(0, source[i]);
            total += source[i].Text.Length;
            if (total >= target) break;
        }

        return overlap;
    }

    private void AddChunk(
        ICollection<DocumentChunk> chunks,
        IReadOnlyList<(int Page, ExtractionLayer Layer, string Text)> current,
        string documentId,
        string sourceFile,
        int chunkIndex)
    {
        var text = string.Join(Environment.NewLine + Environment.NewLine, current.Select(x => x.Text)).Trim();
        if (text.Length < _options.MinChunkLength) return;

        chunks.Add(new DocumentChunk
        {
            ChunkId = Guid.NewGuid().ToString("N"),
            DocumentId = documentId,
            SourceFile = sourceFile,
            PageStart = current.Min(x => x.Page),
            PageEnd = current.Max(x => x.Page),
            ChunkIndex = chunkIndex,
            Text = text,
            ExtractionLayersInvolved = current.Select(x => x.Layer).Distinct().ToList()
        });
    }
}
