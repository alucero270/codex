namespace Codex.Contracts.Documents;

// Phase 1 document payload returned by GET /api/documents/{id}.
public sealed record DocumentResponse(
    long Id,
    string Path,
    string Title,
    string Content,
    DateTime UpdatedAt);
