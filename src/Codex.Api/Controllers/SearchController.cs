using Codex.Api.Data;
using Codex.Contracts.Search;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(
    SearchStore store,
    ILogger<SearchController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResponse>> SearchAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        var queryLength = request.Query?.Length ?? 0;
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        using var scope = logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier,
                ["RequestPath"] = HttpContext.Request.Path.Value,
                ["RequestMethod"] = HttpContext.Request.Method,
                ["RemoteIp"] = remoteIp
            });

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            logger.LogWarning(
                "Rejected search request because the query was empty (query_length: {QueryLength}, requested_limit: {RequestedLimit})",
                queryLength,
                request.Limit);
            ModelState.AddModelError(nameof(request.Query), "Query must not be empty.");
            return ValidationProblem(ModelState);
        }

        // Normalize before query parsing so equivalent inputs behave consistently.
        var query = request.Query.Trim();
        // Bound result window keeps execution predictable and response size small.
        var limit = Math.Clamp(request.Limit, 1, 50);

        var results = await store.SearchAsync(query, limit, cancellationToken);
        logger.LogInformation(
            "Completed search request (query_length: {QueryLength}, result_limit: {ResultLimit}, result_count: {ResultCount})",
            query.Length,
            limit,
            results.Count);
        return Ok(new SearchResponse(query, limit, results));
    }
}
