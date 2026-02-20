using Npgsql;

namespace Codex.Indexer.Data;

public sealed record ClaimedIndexJob(long Id);

public sealed class IndexJobsStore(NpgsqlDataSource dataSource)
{
    // Claim the oldest pending job and transition it to processing atomically.
    private const string ClaimNextPendingJobSql = """
        WITH next_job AS (
            SELECT id
            FROM index_jobs
            WHERE status = 'pending'
            ORDER BY requested_at, id
            FOR UPDATE SKIP LOCKED
            LIMIT 1
        )
        UPDATE index_jobs AS jobs
        SET status = 'processing',
            claimed_at = NOW(),
            worker_id = @worker_id,
            error_message = NULL
        FROM next_job
        WHERE jobs.id = next_job.id
        RETURNING jobs.id;
        """;

    private const string MarkJobCompletedSql = """
        UPDATE index_jobs
        SET status = 'completed',
            completed_at = NOW(),
            error_message = NULL
        WHERE id = @id;
        """;

    private const string MarkJobFailedSql = """
        UPDATE index_jobs
        SET status = 'failed',
            completed_at = NOW(),
            error_message = @error_message
        WHERE id = @id;
        """;

    public async Task<ClaimedIndexJob?> ClaimNextPendingJobAsync(
        string workerId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        // Keep lock scope minimal: claim and commit before any indexing work.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(ClaimNextPendingJobSql, connection, transaction);
        command.Parameters.AddWithValue("worker_id", workerId);

        ClaimedIndexJob? claimedJob;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            claimedJob = await reader.ReadAsync(cancellationToken)
                ? new ClaimedIndexJob(reader.GetInt64(0))
                : null;
        }

        await transaction.CommitAsync(cancellationToken);
        return claimedJob;
    }

    public async Task MarkJobCompletedAsync(long id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(MarkJobCompletedSql);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkJobFailedAsync(
        long id,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(MarkJobFailedSql);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("error_message", errorMessage);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
