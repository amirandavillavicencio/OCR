using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using AppPortable.Search.Mapping;
using Microsoft.Data.Sqlite;

namespace AppPortable.Search.Search;

public sealed class FtsSearchService(string databasePath) : ISearchService
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int take = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT chunk_id, document_id, source_file, page_start, page_end,
       bm25(chunks_fts) AS score,
       snippet(chunks_fts, 5, '[', ']', ' … ', 24) AS snippet
FROM chunks_fts
WHERE chunks_fts MATCH $query
ORDER BY score
LIMIT $take;";
        cmd.Parameters.AddWithValue("$query", query);
        cmd.Parameters.AddWithValue("$take", take);

        var results = new List<SearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(SearchResultMapper.Map(reader));
        }

        return results;
    }
}
