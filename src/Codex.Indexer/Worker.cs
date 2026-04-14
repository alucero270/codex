using Codex.Indexer.Configuration;
using Codex.Indexer.Data;
using Codex.Indexer.Indexing;
using System.Diagnostics;

namespace Codex.Indexer;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    MarkdownDocumentScanner scanner,
    CodexSettings settings) : BackgroundService
{
    private readonly string _workerId =
        $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(settings.PollIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Codex.Indexer started (worker_id: {WorkerId}, docs_root: {DocsRoot}, poll_interval_seconds: {PollIntervalSeconds})",
            _workerId,
            settings.DocsRoot,
            _pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unhandled error in polling loop (worker_id: {WorkerId}, docs_root: {DocsRoot})",
                    _workerId,
                    settings.DocsRoot);
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }

        logger.LogInformation("Codex.Indexer stopping (worker_id: {WorkerId})", _workerId);
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var indexJobsStore = scope.ServiceProvider.GetRequiredService<IndexJobsStore>();
        var documentsStore = scope.ServiceProvider.GetRequiredService<DocumentsStore>();

        var claimedJob =
            await indexJobsStore.ClaimNextPendingJobAsync(_workerId, cancellationToken);
        if (claimedJob is null)
        {
            logger.LogDebug(
                "No pending index job found (worker_id: {WorkerId}, poll_interval_seconds: {PollIntervalSeconds})",
                _workerId,
                _pollInterval.TotalSeconds);
            await Task.Delay(_pollInterval, cancellationToken);
            return;
        }

        using var loggingScope = logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["WorkerId"] = _workerId,
                ["JobId"] = claimedJob.Id,
                ["DocsRoot"] = settings.DocsRoot
            });
        var stopwatch = Stopwatch.StartNew();

        // Claiming commits in the store before this point, so processing is lock-free.
        logger.LogInformation(
            "Claimed index job for processing (attempt_count: {AttemptCount}, max_attempts: {MaxAttempts})",
            claimedJob.AttemptCount,
            claimedJob.MaxAttempts);

        try
        {
            await ProcessClaimedJobAsync(claimedJob, documentsStore, cancellationToken);
            await indexJobsStore.MarkJobCompletedAsync(claimedJob.Id, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "Completed index job (duration_ms: {DurationMs}, attempt_count: {AttemptCount}, max_attempts: {MaxAttempts})",
                stopwatch.ElapsedMilliseconds,
                claimedJob.AttemptCount,
                claimedJob.MaxAttempts);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.GetBaseException().Message;
            // Bound stored error text to avoid excessive row payload size.
            if (errorMessage.Length > 1000)
            {
                errorMessage = errorMessage[..1000];
            }

            var failureDisposition =
                await indexJobsStore.RecordJobFailureAsync(
                    claimedJob.Id,
                    errorMessage,
                    cancellationToken);
            stopwatch.Stop();

            if (failureDisposition.WillRetry)
            {
                logger.LogWarning(
                    ex,
                    "Index job failed and returned to pending for retry " +
                    "(duration_ms: {DurationMs}, attempt_count: {AttemptCount}, " +
                    "max_attempts: {MaxAttempts}, error_message: {ErrorMessage})",
                    stopwatch.ElapsedMilliseconds,
                    failureDisposition.AttemptCount,
                    failureDisposition.MaxAttempts,
                    errorMessage);
            }
            else
            {
                logger.LogError(
                    ex,
                    "Index job failed on final attempt " +
                    "(duration_ms: {DurationMs}, attempt_count: {AttemptCount}, " +
                    "max_attempts: {MaxAttempts}, error_message: {ErrorMessage})",
                    stopwatch.ElapsedMilliseconds,
                    failureDisposition.AttemptCount,
                    failureDisposition.MaxAttempts,
                    errorMessage);
            }
        }
    }

    private async Task ProcessClaimedJobAsync(
        ClaimedIndexJob claimedJob,
        DocumentsStore documentsStore,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Phase 1 behavior: verify server-configured docs root before marking job complete.
        if (!Directory.Exists(settings.DocsRoot))
        {
            logger.LogError(
                "Configured docs root was missing before scan began (docs_root: {DocsRoot})",
                settings.DocsRoot);
            throw new DirectoryNotFoundException(
                $"Configured docs root '{settings.DocsRoot}' does not exist.");
        }

        logger.LogInformation("Scanning configured docs root for markdown content.");
        var scannedDocuments = await scanner.ScanAsync(settings.DocsRoot, cancellationToken);
        var syncResult =
            await documentsStore.SyncDocumentsAsync(scannedDocuments, cancellationToken);

        logger.LogInformation(
            "Synced markdown documents (scanned_count: {ScannedCount}, upserted_count: {UpsertedCount}, deleted_count: {DeletedCount})",
            syncResult.ScannedCount,
            syncResult.UpsertedCount,
            syncResult.DeletedCount);

        logger.LogDebug("Finished indexing work for claimed job.");
    }
}
