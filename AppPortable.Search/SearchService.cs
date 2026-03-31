using AppPortable.Core;
using Microsoft.Data.Sqlite;

namespace AppPortable.Search;

public interface ISearchService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task IndexDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchResult>> SearchAsync(string searchText, int limit = 50, CancellationToken cancellationToken = default);
    Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default);
}

public sealed class SearchService : ISearchService
{
    private readonly string _connectionString;

    public SearchService(string appDataDirectory)
    {
        Directory.CreateDirectory(appDataDirectory);
        var dbPath = Path.Combine(appDataDirectory, "search.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS documents(
    id TEXT PRIMARY KEY,
    source_file TEXT NOT NULL,
    file_name TEXT NOT NULL,
    processed_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS chunks(
    chunk_id TEXT PRIMARY KEY,
    document_id TEXT NOT NULL,
    source_file TEXT NOT NULL,
    page_start INTEGER NOT NULL,
    page_end INTEGER NOT NULL,
    snippet TEXT NOT NULL,
    chunk_text TEXT NOT NULL
);

CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
    chunk_id UNINDEXED,
    document_id UNINDEXED,
    source_file UNINDEXED,
    page_start UNINDEXED,
    page_end UNINDEXED,
    snippet,
    chunk_text
);
";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task IndexDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        await InitializeAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var deleteChunks = connection.CreateCommand();
        deleteChunks.Transaction = transaction;
        deleteChunks.CommandText = "DELETE FROM chunks WHERE document_id = $docId; DELETE FROM chunks_fts WHERE document_id = $docId;";
        deleteChunks.Parameters.AddWithValue("$docId", document.Id);
        await deleteChunks.ExecuteNonQueryAsync(cancellationToken);

        var upsertDocument = connection.CreateCommand();
        upsertDocument.Transaction = transaction;
        upsertDocument.CommandText = @"
INSERT INTO documents (id, source_file, file_name, processed_at_utc)
VALUES ($id, $source_file, $file_name, $processed_at_utc)
ON CONFLICT(id) DO UPDATE SET
source_file = excluded.source_file,
file_name = excluded.file_name,
processed_at_utc = excluded.processed_at_utc;";
        upsertDocument.Parameters.AddWithValue("$id", document.Id);
        upsertDocument.Parameters.AddWithValue("$source_file", document.SourceFile);
        upsertDocument.Parameters.AddWithValue("$file_name", document.FileName);
        upsertDocument.Parameters.AddWithValue("$processed_at_utc", document.ProcessedAtUtc.ToString("O"));
        await upsertDocument.ExecuteNonQueryAsync(cancellationToken);

        foreach (var chunk in document.Chunks)
        {
            var insertChunk = connection.CreateCommand();
            insertChunk.Transaction = transaction;
            insertChunk.CommandText = @"
INSERT INTO chunks (chunk_id, document_id, source_file, page_start, page_end, snippet, chunk_text)
VALUES ($chunk_id, $document_id, $source_file, $page_start, $page_end, $snippet, $chunk_text);";
            insertChunk.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
            insertChunk.Parameters.AddWithValue("$document_id", chunk.DocumentId);
            insertChunk.Parameters.AddWithValue("$source_file", document.SourceFile);
            insertChunk.Parameters.AddWithValue("$page_start", chunk.PageStart);
            insertChunk.Parameters.AddWithValue("$page_end", chunk.PageEnd);
            insertChunk.Parameters.AddWithValue("$snippet", chunk.Snippet);
            insertChunk.Parameters.AddWithValue("$chunk_text", chunk.Text);
            await insertChunk.ExecuteNonQueryAsync(cancellationToken);

            var insertFts = connection.CreateCommand();
            insertFts.Transaction = transaction;
            insertFts.CommandText = @"
INSERT INTO chunks_fts (chunk_id, document_id, source_file, page_start, page_end, snippet, chunk_text)
VALUES ($chunk_id, $document_id, $source_file, $page_start, $page_end, $snippet, $chunk_text);";
            insertFts.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
            insertFts.Parameters.AddWithValue("$document_id", chunk.DocumentId);
            insertFts.Parameters.AddWithValue("$source_file", document.SourceFile);
            insertFts.Parameters.AddWithValue("$page_start", chunk.PageStart);
            insertFts.Parameters.AddWithValue("$page_end", chunk.PageEnd);
            insertFts.Parameters.AddWithValue("$snippet", chunk.Snippet);
            insertFts.Parameters.AddWithValue("$chunk_text", chunk.Text);
            await insertFts.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string searchText, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Array.Empty<SearchResult>();
        }

        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var query = connection.CreateCommand();
        query.CommandText = @"
SELECT chunk_id, document_id, source_file, snippet, chunk_text, page_start, page_end
FROM chunks_fts
WHERE chunks_fts MATCH $query
LIMIT $limit;";
        query.Parameters.AddWithValue("$query", searchText.Trim());
        query.Parameters.AddWithValue("$limit", limit);

        var results = new List<SearchResult>();
        await using var reader = await query.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SearchResult
            {
                ChunkId = reader.GetString(0),
                DocumentId = reader.GetString(1),
                SourceFile = reader.GetString(2),
                Snippet = reader.GetString(3),
                ChunkText = reader.GetString(4),
                PageStart = reader.GetInt32(5),
                PageEnd = reader.GetInt32(6)
            });
        }

        return results;
    }

    public async Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var clear = connection.CreateCommand();
        clear.CommandText = "DELETE FROM chunks; DELETE FROM chunks_fts; DELETE FROM documents;";
        await clear.ExecuteNonQueryAsync(cancellationToken);

        foreach (var document in documents)
        {
            await IndexDocumentAsync(document, cancellationToken);
        }
    }
}
