using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int take = 20, CancellationToken cancellationToken = default);
}
