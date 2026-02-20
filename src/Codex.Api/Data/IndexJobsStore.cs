using Codex.Api.Configuration;
using Codex.Contracts.IndexJobs;
using Npgsql;

namespace Codex.Api.Data;

public sealed class IndexJobsStore(NpgsqlDataSource dataSource, CodexSettings settings)
{
    // Current schema has no root_path column. Reference docs_root in the insert path
    // so job creation still depends on server-side configuration.
    private const string InsertJobSql = """
        WITH configured_root AS (
            SELECT @docs_root::text AS docs_root
        )
        INSERT INTO index_jobs (status)
        SELECT @status
        FROM configured_root
        RETURNING id, status, requested_at, claimed_at, completed_at, worker_id, error_message;
        """;

    private const string SelectJobByIdSql = """
        SELECT id, status, requested_at, claimed_at, completed_at, worker_id, error_message
        FROM index_jobs
        WHERE id = @id;
        """;

    public async Task<IndexJobResponse> CreatePendingJobAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(InsertJobSql);
        command.Parameters.AddWithValue("docs_root", settings.DocsRoot);
        command.Parameters.AddWithValue("status", "pending");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Failed to create index job.");
        }

        // stats is intentionally null until the column exists in a future schema.
        return Map(reader);
    }

    public async Task<IndexJobResponse?> GetJobByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(SelectJobByIdSql);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    private static IndexJobResponse Map(NpgsqlDataReader reader)
    {
        var claimedAtOrdinal = reader.GetOrdinal("claimed_at");
        var completedAtOrdinal = reader.GetOrdinal("completed_at");
        var workerIdOrdinal = reader.GetOrdinal("worker_id");
        var errorMessageOrdinal = reader.GetOrdinal("error_message");

        return new IndexJobResponse(
            Id: reader.GetInt64(reader.GetOrdinal("id")),
            Status: reader.GetString(reader.GetOrdinal("status")),
            RequestedAt: reader.GetDateTime(reader.GetOrdinal("requested_at")),
            ClaimedAt: reader.IsDBNull(claimedAtOrdinal)
                ? null
                : reader.GetDateTime(claimedAtOrdinal),
            CompletedAt: reader.IsDBNull(completedAtOrdinal)
                ? null
                : reader.GetDateTime(completedAtOrdinal),
            WorkerId: reader.IsDBNull(workerIdOrdinal)
                ? null
                : reader.GetString(workerIdOrdinal),
            ErrorMessage: reader.IsDBNull(errorMessageOrdinal)
                ? null
                : reader.GetString(errorMessageOrdinal),
            Stats: null
        );
    }
}
