using Codex.Contracts.Documents;
using Npgsql;

namespace Codex.Api.Data;

public sealed class DocumentsStore(NpgsqlDataSource dataSource)
{
    // Return raw markdown content for viewer endpoints in Phase 1.
    private const string SelectDocumentByIdSql = """
        SELECT id, path, title, content, updated_at
        FROM documents
        WHERE id = @id;
        """;

    public async Task<DocumentResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        // Parameterized by id to keep reads safe and predictable.
        await using var command = dataSource.CreateCommand(SelectDocumentByIdSql);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DocumentResponse(
            Id: reader.GetInt64(reader.GetOrdinal("id")),
            Path: reader.GetString(reader.GetOrdinal("path")),
            Title: reader.GetString(reader.GetOrdinal("title")),
            Content: reader.GetString(reader.GetOrdinal("content")),
            UpdatedAt: reader.GetDateTime(reader.GetOrdinal("updated_at")));
    }
}
