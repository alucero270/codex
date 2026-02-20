using Codex.Contracts.Search;
using Npgsql;

namespace Codex.Api.Data;

public sealed class SearchStore(NpgsqlDataSource dataSource)
{
    // Compute tsquery once, then use precomputed search_vector for indexed matching/ranking.
    private const string SearchSql = """
        WITH parsed_query AS (
            SELECT websearch_to_tsquery('english', @query) AS ts_query
        )
        SELECT
            d.path,
            d.title,
            ts_rank_cd(d.search_vector, parsed_query.ts_query)::double precision AS rank,
            ts_headline(
                'english',
                COALESCE(NULLIF(d.content, ''), d.title),
                parsed_query.ts_query,
                'StartSel=<mark>, StopSel=</mark>, MaxFragments=2, MinWords=8, ' ||
                'MaxWords=18, FragmentDelimiter= ... '
            ) AS snippet
        FROM documents AS d
        CROSS JOIN parsed_query
        WHERE d.search_vector @@ parsed_query.ts_query
        -- Tie-break by path for stable ordering between equal-rank rows.
        ORDER BY rank DESC, d.path ASC
        LIMIT @limit;
        """;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(SearchSql);
        command.Parameters.AddWithValue("query", query);
        command.Parameters.AddWithValue("limit", limit);

        var results = new List<SearchResult>(limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new SearchResult(
                    Path: reader.GetString(reader.GetOrdinal("path")),
                    Title: reader.GetString(reader.GetOrdinal("title")),
                    Snippet: reader.GetString(reader.GetOrdinal("snippet")),
                    Rank: reader.GetDouble(reader.GetOrdinal("rank"))));
        }

        return results;
    }
}
