namespace ComponentesIA.Domain.Entities;

public class ExtractionField
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public required string FieldKey { get; set; }
    public required string Label { get; set; }
    public string? Description { get; set; }
    public required string DataType { get; set; }
    public bool IsRequired { get; set; } = false;
    public string? AliasesJson { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ExampleValue { get; set; }
    public int Order { get; set; } = 0;
    public string? NormalizeRule { get; set; }
    public double ConfidenceThreshold { get; set; } = 0.7;

    public ExtractionTemplate Template { get; set; } = null!;
}
