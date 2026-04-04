using ComponentesIA.Domain.Enums;

namespace ComponentesIA.Domain.Entities;

public class ExtractionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentFileId { get; set; }
    public Guid TemplateId { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int Attempts { get; set; } = 0;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DocumentFile DocumentFile { get; set; } = null!;
    public ExtractionTemplate Template { get; set; } = null!;
    public ExtractionResult? Result { get; set; }
}
