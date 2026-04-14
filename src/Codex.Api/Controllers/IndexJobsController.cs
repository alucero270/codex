using Codex.Api.Data;
using Codex.Contracts.IndexJobs;
using Microsoft.AspNetCore.Mvc;

namespace Codex.Api.Controllers;

[ApiController]
[Route("api/index-jobs")]
public sealed class IndexJobsController(
    IndexJobsStore store,
    ILogger<IndexJobsController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<IndexJobResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<IndexJobResponse>> CreateAsync(
        [FromBody] CreateIndexJobRequest request,
        CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier,
                ["RequestPath"] = HttpContext.Request.Path.Value,
                ["RequestMethod"] = HttpContext.Request.Method
            });

        // Request is intentionally empty for Phase 1.
        _ = request;

        var createdJob = await store.CreatePendingJobAsync(cancellationToken);
        logger.LogInformation(
            "Created indexing job (job_id: {JobId}, status: {JobStatus})",
            createdJob.Id,
            createdJob.Status);
        return Created($"/api/index-jobs/{createdJob.Id}", createdJob);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<IndexJobResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IndexJobResponse>> GetByIdAsync(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier,
                ["RequestPath"] = HttpContext.Request.Path.Value,
                ["RequestMethod"] = HttpContext.Request.Method,
                ["JobId"] = id
            });

        var job = await store.GetJobByIdAsync(id, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Index job lookup returned no result.");
            return NotFound();
        }

        logger.LogInformation(
            "Returned index job status (status: {JobStatus}, worker_id: {WorkerId})",
            job.Status,
            job.WorkerId);
        return Ok(job);
    }
}
