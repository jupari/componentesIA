using System.Security.Cryptography;
using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Domain.Enums;

namespace ComponentesIA.Application.UseCases;

public class DocumentBatchService : IDocumentBatchService
{
    private readonly IDocumentBatchRepository _batchRepo;
    private readonly IExtractionTemplateRepository _templateRepo;
    private readonly IExtractionJobRepository _jobRepo;
    private readonly IDocumentStorageService _storage;
    private readonly IJobDispatcher _dispatcher;
    private readonly ILogger<DocumentBatchService> _logger;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/tiff"
    };

    public DocumentBatchService(
        IDocumentBatchRepository batchRepo,
        IExtractionTemplateRepository templateRepo,
        IExtractionJobRepository jobRepo,
        IDocumentStorageService storage,
        IJobDispatcher dispatcher,
        ILogger<DocumentBatchService> logger)
    {
        _batchRepo = batchRepo;
        _templateRepo = templateRepo;
        _jobRepo = jobRepo;
        _storage = storage;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<BatchResponseDto> CreateBatchAsync(CreateBatchDto dto, IReadOnlyList<IFormFile> files, CancellationToken ct = default)
    {
        var template = await _templateRepo.GetByIdAsync(dto.TemplateId, ct)
            ?? throw new InvalidOperationException($"Plantilla '{dto.TemplateId}' no encontrada.");

        if (files.Count == 0)
            throw new ArgumentException("Debe subir al menos un archivo.");

        foreach (var file in files)
        {
            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException($"Tipo de archivo no permitido: '{file.ContentType}' ({file.FileName}).");

            if (file.Length == 0)
                throw new ArgumentException($"El archivo '{file.FileName}' está vacío.");
        }

        var batch = new DocumentBatch
        {
            TemplateId = dto.TemplateId,
            UploadedBy = dto.UploadedBy,
            TotalDocuments = files.Count,
            Status = BatchStatus.Pending
        };

        await _batchRepo.AddAsync(batch, ct);
        await _batchRepo.SaveChangesAsync(ct);

        var jobSummaries = new List<JobSummaryDto>();

        foreach (var file in files)
        {
            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            var fileBytes = ms.ToArray();

            var sha256 = Convert.ToHexString(SHA256.HashData(fileBytes));
            ms.Seek(0, SeekOrigin.Begin);

            string storagePath;
            try
            {
                storagePath = await _storage.UploadAsync(ms, file.FileName, file.ContentType, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo archivo {FileName} a GCS", file.FileName);
                throw;
            }

            var docFile = new DocumentFile
            {
                BatchId = batch.Id,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                StoragePath = storagePath,
                Sha256 = sha256,
                Status = JobStatus.Pending
            };
            batch.Documents.Add(docFile);

            var job = new ExtractionJob
            {
                DocumentFile = docFile,
                TemplateId = dto.TemplateId,
                Status = JobStatus.Pending
            };
            await _jobRepo.AddAsync(job, ct);

            jobSummaries.Add(new JobSummaryDto
            {
                JobId = job.Id,
                FileName = file.FileName,
                Status = job.Status.ToString(),
                Attempts = job.Attempts,
                CreatedAt = job.CreatedAt
            });
        }

        await _batchRepo.SaveChangesAsync(ct);

        batch.Status = BatchStatus.Processing;
        await _batchRepo.SaveChangesAsync(ct);

        // Dispatch all jobs asynchronously (fire and continue — dispatcher handles errors internally)
        foreach (var summary in jobSummaries)
        {
            _ = Task.Run(() => _dispatcher.DispatchAsync(summary.JobId, CancellationToken.None), CancellationToken.None);
        }

        return new BatchResponseDto
        {
            BatchId = batch.Id,
            TemplateId = batch.TemplateId,
            UploadedBy = batch.UploadedBy,
            Status = batch.Status.ToString(),
            TotalDocuments = batch.TotalDocuments,
            ProcessedDocuments = batch.ProcessedDocuments,
            FailedDocuments = batch.FailedDocuments,
            CreatedAt = batch.CreatedAt,
            Jobs = jobSummaries
        };
    }

    public async Task<List<BatchResponseDto>> GetBatchesAsync(string? uploadedBy = null, CancellationToken ct = default)
    {
        var batches = await _batchRepo.GetAllAsync(uploadedBy, ct);
        return batches.Select(b => new BatchResponseDto
        {
            BatchId            = b.Id,
            TemplateId         = b.TemplateId,
            UploadedBy         = b.UploadedBy,
            Status             = b.Status.ToString(),
            TotalDocuments     = b.TotalDocuments,
            ProcessedDocuments = b.ProcessedDocuments,
            FailedDocuments    = b.FailedDocuments,
            CreatedAt          = b.CreatedAt,
            Jobs               = new List<JobSummaryDto>()
        }).ToList();
    }

    public async Task<BatchResponseDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _batchRepo.GetByIdWithJobsAsync(batchId, ct);
        if (batch is null) return null;

        return new BatchResponseDto
        {
            BatchId = batch.Id,
            TemplateId = batch.TemplateId,
            UploadedBy = batch.UploadedBy,
            Status = batch.Status.ToString(),
            TotalDocuments = batch.TotalDocuments,
            ProcessedDocuments = batch.ProcessedDocuments,
            FailedDocuments = batch.FailedDocuments,
            CreatedAt = batch.CreatedAt,
            Jobs = batch.Documents.Select(d => new JobSummaryDto
            {
                JobId = d.Job?.Id ?? Guid.Empty,
                FileName = d.OriginalFileName,
                Status = d.Job?.Status.ToString() ?? JobStatus.Pending.ToString(),
                Attempts = d.Job?.Attempts ?? 0,
                CreatedAt = d.Job?.CreatedAt ?? d.CreatedAt
            }).ToList()
        };
    }

    public async Task<List<JobSummaryDto>> GetBatchJobsAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _batchRepo.GetByIdWithJobsAsync(batchId, ct);
        if (batch is null) return new List<JobSummaryDto>();

        return batch.Documents.Select(d => new JobSummaryDto
        {
            JobId = d.Job?.Id ?? Guid.Empty,
            FileName = d.OriginalFileName,
            Status = d.Job?.Status.ToString() ?? JobStatus.Pending.ToString(),
            Attempts = d.Job?.Attempts ?? 0,
            CreatedAt = d.Job?.CreatedAt ?? d.CreatedAt
        }).ToList();
    }

    public async Task<List<JobDetailDto>?> GetBatchJobsDetailAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _batchRepo.GetByIdWithJobsDetailAsync(batchId, ct);
        if (batch is null) return null;

        return batch.Documents.Select(d =>
        {
            var job = d.Job;
            return new JobDetailDto
            {
                JobId = job?.Id ?? Guid.Empty,
                FileName = d.OriginalFileName,
                Status = job?.Status.ToString() ?? JobStatus.Pending.ToString(),
                Provider = job?.Provider,
                Model = job?.Model,
                Attempts = job?.Attempts ?? 0,
                StartedAt = job?.StartedAt,
                FinishedAt = job?.FinishedAt,
                ErrorMessage = job?.ErrorMessage,
                Result = job?.Result is null ? null : new JobResultDto
                {
                    ResultId = job.Result.Id,
                    RawResponse = job.Result.RawResponse,
                    NormalizedJson = job.Result.NormalizedJson,
                    ValidationStatus = job.Result.ValidationStatus,
                    ConfidenceScore = job.Result.ConfidenceScore,
                    CreatedAt = job.Result.CreatedAt,
                    Fields = job.Result.FieldResults.Select(f => new FieldResultDto
                    {
                        FieldKey = f.FieldKey,
                        RawValue = f.RawValue,
                        NormalizedValue = f.NormalizedValue,
                        Confidence = f.Confidence,
                        IsValid = f.IsValid,
                        ValidationMessage = f.ValidationMessage
                    }).ToList()
                }
            };
        }).ToList();
    }
}
