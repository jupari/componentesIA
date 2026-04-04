using System.Text.Json;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Domain.Enums;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Application.UseCases;

public class ExtractionJobProcessor : IExtractionJobProcessor
{
    private const int MaxAttempts = 3;

    private readonly AppDbContext _db;
    private readonly IDocumentStorageService _storage;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly IAiExtractionService _aiService;
    private readonly IExtractionValidationService _validator;
    private readonly ILogger<ExtractionJobProcessor> _logger;

    public ExtractionJobProcessor(
        AppDbContext db,
        IDocumentStorageService storage,
        IPromptBuilderService promptBuilder,
        IAiExtractionService aiService,
        IExtractionValidationService validator,
        ILogger<ExtractionJobProcessor> logger)
    {
        _db = db;
        _storage = storage;
        _promptBuilder = promptBuilder;
        _aiService = aiService;
        _validator = validator;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.ExtractionJobs
            .Include(j => j.DocumentFile)
            .Include(j => j.Template)
                .ThenInclude(t => t.Fields.OrderBy(f => f.Order))
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job is null)
        {
            _logger.LogWarning("Job {JobId} no encontrado.", jobId);
            return;
        }

        if (job.Status is JobStatus.Completed or JobStatus.Cancelled)
        {
            _logger.LogInformation("Job {JobId} ya está en estado {Status}, se omite.", jobId, job.Status);
            return;
        }

        if (job.Attempts >= MaxAttempts)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = $"Se alcanzó el máximo de {MaxAttempts} intentos.";
            await UpdateBatchCountersAsync(job.DocumentFile.BatchId, failed: true, ct);
            await _db.SaveChangesAsync(ct);
            return;
        }

        job.Status = JobStatus.Processing;
        job.Attempts++;
        job.StartedAt = DateTime.UtcNow;
        job.Provider = "Google";
        job.Model = job.Template.ModelName ?? _db.Entry(job).CurrentValues["Model"]?.ToString() ?? "gemini";
        await _db.SaveChangesAsync(ct);

        string rawJson;
        try
        {
            var fileBytes = await _storage.DownloadAsync(job.DocumentFile.StoragePath, ct);
            var fields = job.Template.Fields.ToList();

            var systemPrompt = _promptBuilder.BuildSystemPrompt(job.Template, fields);
            var schema = _promptBuilder.BuildExpectedJsonSchema(fields);

            rawJson = await _aiService.ExtractAsync(
                fileBytes,
                job.DocumentFile.ContentType,
                systemPrompt,
                schema,
                ct);
        }
        catch (Exception ex) when (IsRetryable(ex))
        {
            _logger.LogWarning(ex, "Error reintentable en job {JobId} (intento {Attempt})", jobId, job.Attempts);
            job.Status = job.Attempts < MaxAttempts ? JobStatus.Retrying : JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no reintentable en job {JobId}", jobId);
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.FinishedAt = DateTime.UtcNow;
            await UpdateBatchCountersAsync(job.DocumentFile.BatchId, failed: true, ct);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Parse, validate, normalize
        var fields2 = job.Template.Fields.ToList();
        var parsed = _validator.ParseJson(rawJson);
        var validationResults = _validator.Validate(parsed, fields2);
        var normalized = _validator.Normalize(parsed, fields2);

        // Extract confidence scores from raw JSON
        var confidenceMap = ExtractConfidence(rawJson);

        var allValid = validationResults.All(r => r.IsValid);
        var overallConfidence = confidenceMap.Values.Any()
            ? confidenceMap.Values.Average()
            : 0.0;

        var result = new ExtractionResult
        {
            JobId = job.Id,
            RawResponse = rawJson,
            NormalizedJson = JsonSerializer.Serialize(normalized),
            ValidationStatus = allValid ? "Valid" : "Invalid",
            ConfidenceScore = overallConfidence
        };

        foreach (var v in validationResults)
        {
            normalized.TryGetValue(v.FieldKey, out var normVal);
            parsed.TryGetValue(v.FieldKey, out var rawVal);
            confidenceMap.TryGetValue(v.FieldKey, out var conf);

            result.FieldResults.Add(new ExtractionFieldResult
            {
                ResultId = result.Id,
                FieldKey = v.FieldKey,
                RawValue = rawVal?.ToString(),
                NormalizedValue = normVal,
                Confidence = conf,
                IsValid = v.IsValid,
                ValidationMessage = v.Message
            });
        }

        await _db.ExtractionResults.AddAsync(result, ct);

        job.Status = JobStatus.Completed;
        job.FinishedAt = DateTime.UtcNow;
        job.DocumentFile.Status = JobStatus.Completed;

        await UpdateBatchCountersAsync(job.DocumentFile.BatchId, failed: false, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Job {JobId} completado. ValidationStatus: {Status}, Confidence: {Score:F2}",
            jobId, result.ValidationStatus, overallConfidence);
    }

    private async Task UpdateBatchCountersAsync(Guid batchId, bool failed, CancellationToken ct)
    {
        var batch = await _db.DocumentBatches.FindAsync(new object[] { batchId }, ct);
        if (batch is null) return;

        if (failed)
            batch.FailedDocuments++;
        else
            batch.ProcessedDocuments++;

        var total = batch.ProcessedDocuments + batch.FailedDocuments;
        if (total >= batch.TotalDocuments)
        {
            batch.Status = batch.FailedDocuments == 0
                ? BatchStatus.Completed
                : (batch.ProcessedDocuments == 0 ? BatchStatus.Failed : BatchStatus.CompletedWithErrors);
        }

        batch.UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsRetryable(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or TimeoutException ||
        (ex is InvalidOperationException && ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase));

    private static Dictionary<string, double> ExtractConfidence(string rawJson)
    {
        try
        {
            var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.TryGetProperty("fieldConfidence", out var conf))
            {
                return conf.EnumerateObject()
                    .Where(p => p.Value.ValueKind == JsonValueKind.Number)
                    .ToDictionary(p => p.Name, p => p.Value.GetDouble());
            }
        }
        catch { /* ignorar */ }
        return new Dictionary<string, double>();
    }
}
