namespace ComponentesIA.Domain.Entities;

public class ExtractionResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public string? RawResponse { get; set; }
    public string? NormalizedJson { get; set; }
    public string? ValidationStatus { get; set; }
    public double? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ExtractionJob Job { get; set; } = null!;
    public ICollection<ExtractionFieldResult> FieldResults { get; set; } = new List<ExtractionFieldResult>();
}
