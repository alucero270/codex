namespace Codex.Contracts.IndexJobs;

public sealed record IndexJobResponse(
    long Id,
    string Status,
    DateTime RequestedAt,
    DateTime? ClaimedAt,
    DateTime? CompletedAt,
    int AttemptCount,
    int MaxAttempts,
    string? WorkerId,
    string? ErrorMessage,
    object? Stats);
