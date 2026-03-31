using AppPortable.Desktop.Models;

namespace AppPortable.Desktop.Services;

public interface IIndexService
{
    Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task RebuildIndexAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
