using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Enums;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Controllers;

[ApiController]
[Route("api/extraction/jobs")]
public class ExtractionJobsController : ControllerBase
{
    private readonly IExtractionJobRepository _jobRepo;
    private readonly IJobDispatcher _dispatcher;
    private readonly AppDbContext _db;

    public ExtractionJobsController(
        IExtractionJobRepository jobRepo,
        IJobDispatcher dispatcher,
        AppDbContext db)
    {
        _jobRepo = jobRepo;
        _dispatcher = dispatcher;
        _db = db;
    }

    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(JobDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob(Guid jobId, CancellationToken ct)
    {
        var job = await _jobRepo.GetByIdWithResultAsync(jobId, ct);
        if (job is null) return NotFound();

        return Ok(MapToDetail(job));
    }

    [HttpGet("{jobId:guid}/result")]
    [ProducesResponseType(typeof(JobResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetResult(Guid jobId, CancellationToken ct)
    {
        var job = await _jobRepo.GetByIdWithResultAsync(jobId, ct);
        if (job is null) return NotFound();
        if (job.Result is null) return NoContent();

        return Ok(MapToResult(job.Result));
    }

    [HttpPost("{jobId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(Guid jobId, CancellationToken ct)
    {
        var job = await _jobRepo.GetByIdAsync(jobId, ct);
        if (job is null) return NotFound();

        if (job.Status is JobStatus.Processing or JobStatus.Queued)
            return Conflict(new { message = "El job ya está en proceso." });

        if (job.Status is JobStatus.Completed)
            return Conflict(new { message = "El job ya fue completado exitosamente." });

        const int maxAttempts = 3;
        if (job.Attempts >= maxAttempts)
            return Conflict(new { message = $"El job alcanzó el máximo de {maxAttempts} intentos." });

        job.Status = JobStatus.Queued;
        job.ErrorMessage = null;
        await _jobRepo.SaveChangesAsync(ct);

        await _dispatcher.DispatchAsync(jobId, ct);

        return Accepted(new { jobId, message = "Job re-encolado." });
    }

    [HttpGet("{jobId:guid}/audit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAudit(Guid jobId, CancellationToken ct)
    {
        var job = await _db.ExtractionJobs
            .Include(j => j.DocumentFile)
            .Include(j => j.Template)
            .Include(j => j.Result)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job is null) return NotFound();

        var audit = new
        {
            jobId = job.Id,
            status = job.Status.ToString(),
            attempts = job.Attempts,
            provider = job.Provider,
            model = job.Model,
            promptVersion = job.PromptVersion,
            startedAt = job.StartedAt,
            finishedAt = job.FinishedAt,
            errorMessage = job.ErrorMessage,
            document = new
            {
                fileName = job.DocumentFile.OriginalFileName,
                contentType = job.DocumentFile.ContentType,
                sha256 = job.DocumentFile.Sha256,
                storagePath = job.DocumentFile.StoragePath
            },
            template = new
            {
                id = job.Template.Id,
                name = job.Template.Name,
                code = job.Template.Code
            },
            result = job.Result is null ? null : new
            {
                rawResponseLength = job.Result.RawResponse?.Length,
                validationStatus = job.Result.ValidationStatus,
                confidenceScore = job.Result.ConfidenceScore,
                createdAt = job.Result.CreatedAt
            }
        };

        return Ok(audit);
    }

    private static JobDetailDto MapToDetail(Domain.Entities.ExtractionJob job) => new()
    {
        JobId = job.Id,
        Status = job.Status.ToString(),
        FileName = job.DocumentFile.OriginalFileName,
        Provider = job.Provider,
        Model = job.Model,
        Attempts = job.Attempts,
        StartedAt = job.StartedAt,
        FinishedAt = job.FinishedAt,
        ErrorMessage = job.ErrorMessage,
        Result = job.Result is null ? null : MapToResult(job.Result)
    };

    private static JobResultDto MapToResult(Domain.Entities.ExtractionResult result) => new()
    {
        ResultId = result.Id,
        RawResponse = result.RawResponse,
        NormalizedJson = result.NormalizedJson,
        ValidationStatus = result.ValidationStatus,
        ConfidenceScore = result.ConfidenceScore,
        CreatedAt = result.CreatedAt,
        Fields = result.FieldResults.Select(f => new FieldResultDto
        {
            FieldKey = f.FieldKey,
            RawValue = f.RawValue,
            NormalizedValue = f.NormalizedValue,
            Confidence = f.Confidence,
            IsValid = f.IsValid,
            ValidationMessage = f.ValidationMessage
        }).ToList()
    };
}
