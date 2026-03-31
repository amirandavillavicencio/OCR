using AppPortable.Desktop.Models;
using Microsoft.Data.Sqlite;

namespace AppPortable.Desktop.Services;

public sealed class SqliteFtsSearchService : ISearchService
{
    private readonly string _dbPath;

    public SqliteFtsSearchService()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppPortable", "data");
        Directory.CreateDirectory(baseDir);
        _dbPath = Path.Combine(baseDir, "search.db");
        EnsureSchema();
    }

    public async Task IndexDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM chunks_fts WHERE doc_id = $docId;";
        deleteCmd.Parameters.AddWithValue("$docId", document.Id);
        await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

        foreach (var chunk in document.Chunks)
        {
            var insert = connection.CreateCommand();
            insert.CommandText = @"INSERT INTO chunks_fts (doc_id, source_file, page_start, page_end, snippet, text)
                                   VALUES ($docId, $source, $pageStart, $pageEnd, $snippet, $text);";
            insert.Parameters.AddWithValue("$docId", document.Id);
            insert.Parameters.AddWithValue("$source", document.SourceFile);
            insert.Parameters.AddWithValue("$pageStart", chunk.PageStart);
            insert.Parameters.AddWithValue("$pageEnd", chunk.PageEnd);
            insert.Parameters.AddWithValue("$snippet", chunk.Snippet);
            insert.Parameters.AddWithValue("$text", chunk.Text);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var clear = connection.CreateCommand();
        clear.CommandText = "DELETE FROM chunks_fts;";
        await clear.ExecuteNonQueryAsync(cancellationToken);

        foreach (var document in documents)
        {
            foreach (var chunk in document.Chunks)
            {
                var insert = connection.CreateCommand();
                insert.CommandText = @"INSERT INTO chunks_fts (doc_id, source_file, page_start, page_end, snippet, text)
                                       VALUES ($docId, $source, $pageStart, $pageEnd, $snippet, $text);";
                insert.Parameters.AddWithValue("$docId", document.Id);
                insert.Parameters.AddWithValue("$source", document.SourceFile);
                insert.Parameters.AddWithValue("$pageStart", chunk.PageStart);
                insert.Parameters.AddWithValue("$pageEnd", chunk.PageEnd);
                insert.Parameters.AddWithValue("$snippet", chunk.Snippet);
                insert.Parameters.AddWithValue("$text", chunk.Text);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<SearchResultItem>> SearchAsync(string query, int limit = 100, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT doc_id, source_file, page_start, page_end, snippet, text
                            FROM chunks_fts
                            WHERE chunks_fts MATCH $query
                            LIMIT $limit;";
        cmd.Parameters.AddWithValue("$query", query);
        cmd.Parameters.AddWithValue("$limit", limit);

        var results = new List<SearchResultItem>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SearchResultItem
            {
                DocumentId = reader.GetString(0),
                SourceFile = reader.GetString(1),
                PageStart = reader.GetInt32(2),
                PageEnd = reader.GetInt32(3),
                Snippet = reader.GetString(4),
                FullText = reader.GetString(5)
            });
        }

        return results;
    }

    private SqliteConnection OpenConnection() => new($"Data Source={_dbPath}");

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
                                doc_id UNINDEXED,
                                source_file,
                                page_start UNINDEXED,
                                page_end UNINDEXED,
                                snippet,
                                text
                            );";
        cmd.ExecuteNonQuery();
    }
}
