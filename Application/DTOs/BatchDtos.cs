using System.ComponentModel.DataAnnotations;
using ComponentesIA.Domain.Enums;

namespace ComponentesIA.Application.DTOs;

public class CreateBatchDto
{
    [Required]
    public Guid TemplateId { get; set; }

    public string? UploadedBy { get; set; }
}

public class BatchResponseDto
{
    public Guid BatchId { get; set; }
    public Guid TemplateId { get; set; }
    public string? UploadedBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int ProcessedDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<JobSummaryDto> Jobs { get; set; } = new();
}

public class JobSummaryDto
{
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobDetailDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public int Attempts { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public JobResultDto? Result { get; set; }
}

public class JobResultDto
{
    public Guid ResultId { get; set; }
    public string? RawResponse { get; set; }
    public string? NormalizedJson { get; set; }
    public string? ValidationStatus { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FieldResultDto> Fields { get; set; } = new();
}

public class FieldResultDto
{
    public string FieldKey { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? NormalizedValue { get; set; }
    public double? Confidence { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
}
