using AppPortable.Desktop.Models;
using Microsoft.Data.Sqlite;

namespace AppPortable.Desktop.Services;

public sealed class SqliteIndexService : IIndexService
{
    private readonly string _connectionString;

    public SqliteIndexService(string? dataRoot = null)
    {
        var root = dataRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppPortable");
        Directory.CreateDirectory(root);
        var dbPath = Path.Combine(root, "search.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        EnsureDatabase();
    }

    public async Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var chunk in chunks)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO chunks(chunk_id, document_id, document_name, chunk_number, content)
                VALUES ($chunkId, $documentId, $documentName, $chunkNumber, $content)
                ON CONFLICT(chunk_id) DO UPDATE SET
                    document_name = excluded.document_name,
                    content = excluded.content,
                    chunk_number = excluded.chunk_number;
                """;
            command.Parameters.AddWithValue("$chunkId", chunk.ChunkId);
            command.Parameters.AddWithValue("$documentId", chunk.DocumentId);
            command.Parameters.AddWithValue("$documentName", chunk.DocumentName);
            command.Parameters.AddWithValue("$chunkNumber", chunk.ChunkNumber);
            command.Parameters.AddWithValue("$content", chunk.Content);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RebuildIndexAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM chunks;";
        await delete.ExecuteNonQueryAsync(cancellationToken);

        await IndexChunksAsync(chunks, cancellationToken);
    }

    private void EnsureDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS chunks USING fts5(
                chunk_id UNINDEXED,
                document_id UNINDEXED,
                document_name,
                chunk_number UNINDEXED,
                content
            );
            """;
        command.ExecuteNonQuery();
    }
}
