using Codex.Indexer.Configuration;
using Codex.Indexer.Data;

namespace Codex.Indexer;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    CodexSettings settings) : BackgroundService
{
    private readonly string _workerId =
        $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(settings.PollIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Codex.Indexer started (worker_id: {WorkerId}, docs_root: {DocsRoot})",
            _workerId,
            settings.DocsRoot);

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
                logger.LogError(ex, "Unhandled error in polling loop.");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }

        logger.LogInformation("Codex.Indexer stopping (worker_id: {WorkerId})", _workerId);
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IndexJobsStore>();

        var claimedJob = await store.ClaimNextPendingJobAsync(_workerId, cancellationToken);
        if (claimedJob is null)
        {
            await Task.Delay(_pollInterval, cancellationToken);
            return;
        }

        // Claiming commits in the store before this point, so processing is lock-free.
        logger.LogInformation("Claimed index job {JobId}", claimedJob.Id);

        try
        {
            await ProcessClaimedJobAsync(claimedJob, cancellationToken);
            await store.MarkJobCompletedAsync(claimedJob.Id, cancellationToken);
            logger.LogInformation("Completed index job {JobId}", claimedJob.Id);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.GetBaseException().Message;
            // Bound stored error text to avoid excessive row payload size.
            if (errorMessage.Length > 1000)
            {
                errorMessage = errorMessage[..1000];
            }

            await store.MarkJobFailedAsync(claimedJob.Id, errorMessage, cancellationToken);
            logger.LogError(ex, "Failed index job {JobId}", claimedJob.Id);
        }
    }

    private Task ProcessClaimedJobAsync(
        ClaimedIndexJob claimedJob,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Phase 1 behavior: verify server-configured docs root before marking job complete.
        if (!Directory.Exists(settings.DocsRoot))
        {
            throw new DirectoryNotFoundException(
                $"Configured docs root '{settings.DocsRoot}' does not exist.");
        }

        logger.LogDebug("Processed placeholder indexing for job {JobId}.", claimedJob.Id);

        return Task.CompletedTask;
    }
}
