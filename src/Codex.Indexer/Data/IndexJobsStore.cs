using Npgsql;

namespace Codex.Indexer.Data;

public sealed record ClaimedIndexJob(long Id, int AttemptCount, int MaxAttempts);

public sealed record JobFailureDisposition(
    long Id,
    string Status,
    int AttemptCount,
    int MaxAttempts)
{
    public bool WillRetry => string.Equals(Status, "pending", StringComparison.Ordinal);
}

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
            completed_at = NULL,
            worker_id = @worker_id,
            error_message = NULL,
            attempt_count = jobs.attempt_count + 1
        FROM next_job
        WHERE jobs.id = next_job.id
        RETURNING jobs.id, jobs.attempt_count, jobs.max_attempts;
        """;

    private const string MarkJobCompletedSql = """
        UPDATE index_jobs
        SET status = 'completed',
            completed_at = NOW(),
            error_message = NULL
        WHERE id = @id;
        """;

    private const string RecordJobFailureSql = """
        UPDATE index_jobs
        SET status = CASE
                WHEN attempt_count < max_attempts THEN 'pending'
                ELSE 'failed'
            END,
            claimed_at = CASE
                WHEN attempt_count < max_attempts THEN NULL
                ELSE claimed_at
            END,
            completed_at = CASE
                WHEN attempt_count < max_attempts THEN NULL
                ELSE NOW()
            END,
            worker_id = CASE
                WHEN attempt_count < max_attempts THEN NULL
                ELSE worker_id
            END,
            error_message = @error_message
        WHERE id = @id
        RETURNING id, status, attempt_count, max_attempts;
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
                ? new ClaimedIndexJob(
                    reader.GetInt64(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2))
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

    public async Task<JobFailureDisposition> RecordJobFailureAsync(
        long id,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(RecordJobFailureSql);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("error_message", errorMessage);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Failed to record failure for job {id}.");
        }

        return new JobFailureDisposition(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetInt32(3));
    }
}
