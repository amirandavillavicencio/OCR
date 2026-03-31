using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunk> Chunk(string documentId, string sourceFile, IReadOnlyList<DocumentPage> pages);
}
