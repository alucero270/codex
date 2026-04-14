using Npgsql;

namespace Codex.Indexer.Data;

public sealed record PersistedSource(
    long Id,
    string Name,
    string Type,
    string RootPath,
    string Status,
    DateTime? LastIndexed);

public sealed class SourcesStore(NpgsqlDataSource dataSource)
{
    private const string EnsureConfiguredFilesystemSourceSql = """
        INSERT INTO sources (name, type, root_path, status)
        VALUES (@name, @type, @root_path, @status)
        ON CONFLICT (root_path) DO UPDATE
        SET name = EXCLUDED.name,
            type = EXCLUDED.type,
            status = EXCLUDED.status
        RETURNING id, name, type, root_path, status, last_indexed;
        """;

    public async Task<PersistedSource> EnsureConfiguredFilesystemSourceAsync(
        string sourceName,
        string docsRoot,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(EnsureConfiguredFilesystemSourceSql);
        command.Parameters.AddWithValue("name", sourceName);
        command.Parameters.AddWithValue("type", "filesystem");
        command.Parameters.AddWithValue("root_path", docsRoot);
        command.Parameters.AddWithValue("status", "active");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to ensure configured filesystem source.");
        }

        return new PersistedSource(
            Id: reader.GetInt64(reader.GetOrdinal("id")),
            Name: reader.GetString(reader.GetOrdinal("name")),
            Type: reader.GetString(reader.GetOrdinal("type")),
            RootPath: reader.GetString(reader.GetOrdinal("root_path")),
            Status: reader.GetString(reader.GetOrdinal("status")),
            LastIndexed: reader.IsDBNull(reader.GetOrdinal("last_indexed"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("last_indexed")));
    }
}
