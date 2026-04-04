using System.ComponentModel.DataAnnotations;

namespace ComponentesIA.Application.DTOs;

public class CreateTemplateDto
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required, MaxLength(100)]
    public required string Code { get; set; }

    public string? Description { get; set; }
    public string? AiProvider { get; set; }
    public string? ModelName { get; set; }
    public string? PromptStrategy { get; set; }

    public List<CreateFieldDto> Fields { get; set; } = new();
}

public class CreateFieldDto
{
    [Required, MaxLength(100)]
    public required string FieldKey { get; set; }

    [Required, MaxLength(200)]
    public required string Label { get; set; }

    public string? Description { get; set; }

    [Required, MaxLength(50)]
    public string DataType { get; set; } = "string";

    public bool IsRequired { get; set; } = false;
    public string? AliasesJson { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ExampleValue { get; set; }
    public int Order { get; set; } = 0;
    public string? NormalizeRule { get; set; }
    public double ConfidenceThreshold { get; set; } = 0.7;
}

public class UpdateTemplateDto
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }

    public string? Description { get; set; }
    public string? AiProvider { get; set; }
    public string? ModelName { get; set; }
    public string? PromptStrategy { get; set; }
    public bool IsActive { get; set; } = true;

    public List<CreateFieldDto> Fields { get; set; } = new();
}

public class TemplateResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AiProvider { get; set; }
    public string? ModelName { get; set; }
    public string? PromptStrategy { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FieldResponseDto> Fields { get; set; } = new();
}

public class FieldResponseDto
{
    public Guid Id { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? AliasesJson { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ExampleValue { get; set; }
    public int Order { get; set; }
    public string? NormalizeRule { get; set; }
    public double ConfidenceThreshold { get; set; }
}
