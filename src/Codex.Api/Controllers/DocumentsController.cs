using Codex.Api.Data;
using Codex.Contracts.Documents;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(DocumentsStore store) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentResponse>> GetByIdAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var document = await store.GetByIdAsync(id, cancellationToken);
        // Missing ids map to 404 for viewer callers.
        return document is null ? NotFound() : Ok(document);
    }
}
