using AppPortable.Desktop.Models;

namespace AppPortable.Desktop.Services;

public interface ISearchService
{
    Task IndexDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default);
    Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(string query, int limit = 100, CancellationToken cancellationToken = default);
}
