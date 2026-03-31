using AppPortable.Desktop.Models;

namespace AppPortable.Desktop.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
}
