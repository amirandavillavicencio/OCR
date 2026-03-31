using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using Microsoft.Data.Sqlite;

namespace AppPortable.Search.Indexing;

public sealed class FtsIndexService(string databasePath) : IIndexService
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
  chunk_id UNINDEXED,
  document_id UNINDEXED,
  source_file UNINDEXED,
  page_start UNINDEXED,
  page_end UNINDEXED,
  text
);
";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task IndexAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = connection.BeginTransaction();

        var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM chunks_fts WHERE document_id = $document_id;";
        delete.Parameters.AddWithValue("$document_id", document.DocumentId);
        await delete.ExecuteNonQueryAsync(cancellationToken);

        foreach (var chunk in document.Chunks)
        {
            var insert = connection.CreateCommand();
            insert.CommandText = @"INSERT INTO chunks_fts(chunk_id, document_id, source_file, page_start, page_end, text)
VALUES($chunk_id, $document_id, $source_file, $page_start, $page_end, $text);";
            insert.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
            insert.Parameters.AddWithValue("$document_id", chunk.DocumentId);
            insert.Parameters.AddWithValue("$source_file", chunk.SourceFile);
            insert.Parameters.AddWithValue("$page_start", chunk.PageStart);
            insert.Parameters.AddWithValue("$page_end", chunk.PageEnd);
            insert.Parameters.AddWithValue("$text", chunk.Text);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task ReindexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var clear = connection.CreateCommand();
        clear.CommandText = "DELETE FROM chunks_fts;";
        await clear.ExecuteNonQueryAsync(cancellationToken);

        foreach (var doc in documents)
        {
            await IndexAsync(doc, cancellationToken);
        }
    }
}
