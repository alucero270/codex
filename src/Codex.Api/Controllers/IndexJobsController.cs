using Codex.Api.Data;
using Codex.Contracts.IndexJobs;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Api.Controllers;

[ApiController]
[Route("api/index-jobs")]
public sealed class IndexJobsController(IndexJobsStore store) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<IndexJobResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IndexJobResponse>> CreateAsync(
        [FromBody] CreateIndexJobRequest request,
        CancellationToken cancellationToken)
    {
        // Request is intentionally empty for Phase 1.
        _ = request;

        var createdJob = await store.CreatePendingJobAsync(cancellationToken);
        return Created($"/api/index-jobs/{createdJob.Id}", createdJob);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<IndexJobResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IndexJobResponse>> GetByIdAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var job = await store.GetJobByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }
}
