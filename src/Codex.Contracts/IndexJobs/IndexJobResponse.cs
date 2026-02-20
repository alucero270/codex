namespace Codex.Contracts.IndexJobs;

public sealed record IndexJobResponse(
    long Id,
    string Status,
    DateTime RequestedAt,
    DateTime? ClaimedAt,
    DateTime? CompletedAt,
    string? WorkerId,
    string? ErrorMessage,
    object? Stats);
