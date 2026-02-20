namespace Codex.Contracts.Search;

public sealed record SearchResult(
    long Id,
    string Path,
    string Title,
    string Snippet,
    double Rank);
