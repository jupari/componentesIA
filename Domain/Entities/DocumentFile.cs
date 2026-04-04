using ComponentesIA.Domain.Enums;

namespace ComponentesIA.Domain.Entities;

public class DocumentFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public required string StoragePath { get; set; }
    public string? Sha256 { get; set; }
    public int? PageCount { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DocumentBatch Batch { get; set; } = null!;
    public ExtractionJob? Job { get; set; }
}
