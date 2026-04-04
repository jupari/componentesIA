namespace ComponentesIA.Domain.Entities;

public class ExtractionFieldResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResultId { get; set; }
    public required string FieldKey { get; set; }
    public string? RawValue { get; set; }
    public string? NormalizedValue { get; set; }
    public double? Confidence { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ValidationMessage { get; set; }
    public int? PageNumber { get; set; }
    public string? BoundingBoxJson { get; set; }

    public ExtractionResult Result { get; set; } = null!;
}
