using ComponentesIA.Domain.Enums;

namespace ComponentesIA.Domain.Entities;

public class DocumentBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public string? UploadedBy { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Pending;
    public int TotalDocuments { get; set; } = 0;
    public int ProcessedDocuments { get; set; } = 0;
    public int FailedDocuments { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ExtractionTemplate Template { get; set; } = null!;
    public ICollection<DocumentFile> Documents { get; set; } = new List<DocumentFile>();
}
