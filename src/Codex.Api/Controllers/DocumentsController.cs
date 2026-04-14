using Codex.Api.Data;
using Codex.Contracts.Documents;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    DocumentsStore store,
    ILogger<DocumentsController> logger) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetByIdAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier,
                ["RequestPath"] = HttpContext.Request.Path.Value,
                ["RequestMethod"] = HttpContext.Request.Method,
                ["DocumentId"] = id
            });

        var document = await store.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            logger.LogWarning("Document lookup returned no result.");
            return NotFound();
        }

        logger.LogInformation("Returned document response.");
        return Ok(document);
    }
}
