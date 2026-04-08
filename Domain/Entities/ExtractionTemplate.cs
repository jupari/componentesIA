namespace ComponentesIA.Domain.Entities;

public class ExtractionTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public string? AiProvider { get; set; }
    public string? ModelName { get; set; }
    public string? PromptStrategy { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SampleDocumentPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExtractionField> Fields { get; set; } = new List<ExtractionField>();
    public ICollection<DocumentBatch> Batches { get; set; } = new List<DocumentBatch>();
}
