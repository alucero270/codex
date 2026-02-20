namespace Codex.Contracts.Search;

public sealed record SearchResult(
    string Path,
    string Title,
    string Snippet,
    double Rank);
