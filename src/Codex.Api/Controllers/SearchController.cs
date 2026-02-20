using Codex.Api.Data;
using Codex.Contracts.Search;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(SearchStore store) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResponse>> SearchAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            ModelState.AddModelError(nameof(request.Query), "Query must not be empty.");
            return ValidationProblem(ModelState);
        }

        // Normalize before query parsing so equivalent inputs behave consistently.
        var query = request.Query.Trim();
        // Bound result window keeps execution predictable and response size small.
        var limit = Math.Clamp(request.Limit, 1, 50);

        var results = await store.SearchAsync(query, limit, cancellationToken);
        return Ok(new SearchResponse(query, limit, results));
    }
}
