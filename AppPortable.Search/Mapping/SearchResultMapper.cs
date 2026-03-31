using AppPortable.Core.Models;
using Microsoft.Data.Sqlite;

namespace AppPortable.Search.Mapping;

public static class SearchResultMapper
{
    public static SearchResult Map(SqliteDataReader reader)
    {
        return new SearchResult
        {
            ChunkId = reader.GetString(0),
            DocumentId = reader.GetString(1),
            SourceFile = reader.GetString(2),
            PageStart = reader.GetInt32(3),
            PageEnd = reader.GetInt32(4),
            Score = reader.GetDouble(5),
            Snippet = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
        };
    }
}
