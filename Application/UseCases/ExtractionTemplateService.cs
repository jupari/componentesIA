using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Application.UseCases;

public class ExtractionTemplateService : IExtractionTemplateService
{
    private readonly IExtractionTemplateRepository _repository;
    private readonly IDocumentStorageService _storage;

    public ExtractionTemplateService(IExtractionTemplateRepository repository, IDocumentStorageService storage)
    {
        _repository = repository;
        _storage = storage;
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

        // Step 1: delete existing fields and commit — runs DELETE + UPDATE in isolation
        _repository.RemoveFields(template.Fields.ToList());
        await _repository.SaveChangesAsync(ct);

        // Step 2: insert new fields via DbSet directly — avoids re-attaching Detached entities
        // (calling template.Fields.Clear() after AcceptAllChanges would re-attach Deleted→Detached
        // items and cause a second DELETE that affects 0 rows → DbUpdateConcurrencyException)
        var newFields = dto.Fields.Select(f => MapToField(f, template.Id)).ToList();
        _repository.AddFields(newFields);
        await _repository.SaveChangesAsync(ct);

        // Re-query to return fresh data with the new fields
        template = (await _repository.GetByIdWithFieldsAsync(id, ct))!;
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

    public async Task<TemplateResponseDto?> UploadSampleAsync(Guid id, IFormFile file, CancellationToken ct = default)
    {
        var template = await _repository.GetByIdWithFieldsAsync(id, ct);
        if (template is null) return null;

        using var stream = file.OpenReadStream();
        var storagePath = await _storage.UploadAsync(stream, $"templates/{id}/sample/{file.FileName}", file.ContentType, ct);

        template.SampleDocumentPath = storagePath;
        template.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync(ct);

        return MapToResponse(template);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)?> GetSampleAsync(Guid id, CancellationToken ct = default)
    {
        var template = await _repository.GetByIdWithFieldsAsync(id, ct);
        if (template is null || string.IsNullOrEmpty(template.SampleDocumentPath)) return null;

        var bytes = await _storage.DownloadAsync(template.SampleDocumentPath, ct);
        var fileName = Path.GetFileName(template.SampleDocumentPath);
        return (bytes, "application/pdf", fileName);
    }

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
        SampleDocumentPath = t.SampleDocumentPath,
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
