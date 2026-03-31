using AppPortable.Desktop.Models;
using Microsoft.Data.Sqlite;

namespace AppPortable.Desktop.Services;

public sealed class SqliteSearchService : ISearchService
{
    private readonly string _connectionString;

    public SqliteSearchService(string? dataRoot = null)
    {
        var root = dataRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppPortable");
        var dbPath = Path.Combine(root, "search.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
    }

    public async Task<IReadOnlyList<SearchResultItem>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchResultItem>();
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT chunk_id, document_name, chunk_number, content, bm25(chunks) AS score
            FROM chunks
            WHERE chunks MATCH $query
            ORDER BY score
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$query", query);
        command.Parameters.AddWithValue("$limit", limit);

        var result = new List<SearchResultItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SearchResultItem
            {
                ChunkId = reader.GetString(0),
                DocumentName = reader.GetString(1),
                ChunkNumber = reader.GetInt32(2),
                Content = reader.GetString(3),
                Score = reader.GetDouble(4)
            });
        }

        return result;
    }
}
