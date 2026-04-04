using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Application.UseCases;

public class ExtractionTemplateService : IExtractionTemplateService
{
    private readonly IExtractionTemplateRepository _repository;

    public ExtractionTemplateService(IExtractionTemplateRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TemplateResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var templates = await _repository.GetAllActiveAsync(ct);
        return templates.Select(MapToResponse).ToList();
    }

    public async Task<TemplateResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var template = await _repository.GetByIdWithFieldsAsync(id, ct);
        return template is null ? null : MapToResponse(template);
    }

    public async Task<TemplateResponseDto> CreateAsync(CreateTemplateDto dto, CancellationToken ct = default)
    {
        if (await _repository.ExistsCodeAsync(dto.Code, ct: ct))
            throw new InvalidOperationException($"Ya existe una plantilla con el código '{dto.Code}'.");

        var template = new ExtractionTemplate
        {
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            AiProvider = dto.AiProvider,
            ModelName = dto.ModelName,
            PromptStrategy = dto.PromptStrategy
        };

        foreach (var f in dto.Fields)
            template.Fields.Add(MapToField(f, template.Id));

        await _repository.AddAsync(template, ct);
        await _repository.SaveChangesAsync(ct);

        return MapToResponse(template);
    }

    public async Task<TemplateResponseDto?> UpdateAsync(Guid id, UpdateTemplateDto dto, CancellationToken ct = default)
    {
        var template = await _repository.GetByIdWithFieldsAsync(id, ct);
        if (template is null) return null;

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.AiProvider = dto.AiProvider;
        template.ModelName = dto.ModelName;
        template.PromptStrategy = dto.PromptStrategy;
        template.IsActive = dto.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        // Replace fields entirely
        template.Fields.Clear();
        foreach (var f in dto.Fields)
            template.Fields.Add(MapToField(f, template.Id));

        await _repository.SaveChangesAsync(ct);
        return MapToResponse(template);
    }

    private static ExtractionField MapToField(CreateFieldDto f, Guid templateId) => new()
    {
        TemplateId = templateId,
        FieldKey = f.FieldKey,
        Label = f.Label,
        Description = f.Description,
        DataType = f.DataType,
        IsRequired = f.IsRequired,
        AliasesJson = f.AliasesJson,
        ValidationRegex = f.ValidationRegex,
        ExampleValue = f.ExampleValue,
        Order = f.Order,
        NormalizeRule = f.NormalizeRule,
        ConfidenceThreshold = f.ConfidenceThreshold
    };

    private static TemplateResponseDto MapToResponse(ExtractionTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Code = t.Code,
        Description = t.Description,
        AiProvider = t.AiProvider,
        ModelName = t.ModelName,
        PromptStrategy = t.PromptStrategy,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        Fields = t.Fields.OrderBy(f => f.Order).Select(f => new FieldResponseDto
        {
            Id = f.Id,
            FieldKey = f.FieldKey,
            Label = f.Label,
            Description = f.Description,
            DataType = f.DataType,
            IsRequired = f.IsRequired,
            AliasesJson = f.AliasesJson,
            ValidationRegex = f.ValidationRegex,
            ExampleValue = f.ExampleValue,
            Order = f.Order,
            NormalizeRule = f.NormalizeRule,
            ConfidenceThreshold = f.ConfidenceThreshold
        }).ToList()
    };
}
