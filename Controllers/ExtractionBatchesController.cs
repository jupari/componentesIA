using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComponentesIA.Controllers;

[ApiController]
[Route("api/extraction/batches")]
public class ExtractionBatchesController : ControllerBase
{
    private readonly IDocumentBatchService _batchService;

    public ExtractionBatchesController(IDocumentBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BatchResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBatch(
        [FromForm] CreateBatchDto dto,
        [FromForm] IReadOnlyList<IFormFile> files,
        CancellationToken ct)
    {
        try
        {
            var result = await _batchService.CreateBatchAsync(dto, files, ct);
            return AcceptedAtAction(nameof(GetBatch), new { batchId = result.BatchId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{batchId:guid}")]
    [ProducesResponseType(typeof(BatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(Guid batchId, CancellationToken ct)
    {
        var result = await _batchService.GetBatchAsync(batchId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{batchId:guid}/jobs")]
    [ProducesResponseType(typeof(List<JobSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchJobs(Guid batchId, CancellationToken ct)
    {
        var jobs = await _batchService.GetBatchJobsAsync(batchId, ct);
        return Ok(jobs);
    }

    [HttpGet("{batchId:guid}/jobs/details")]
    [ProducesResponseType(typeof(List<JobDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchJobsDetail(Guid batchId, CancellationToken ct)
    {
        var jobs = await _batchService.GetBatchJobsDetailAsync(batchId, ct);
        return jobs is null ? NotFound() : Ok(jobs);
    }
}
