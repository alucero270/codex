namespace Codex.Contracts.Search;

public sealed record SearchResponse(
    string Query,
    int Limit,
    IReadOnlyList<SearchResult> Results);
