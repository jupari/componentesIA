using ComponentesIA.Application.DTOs;

namespace ComponentesIA.Application.Interfaces;

public interface IExtractionTemplateService
{
    Task<List<TemplateResponseDto>> GetAllAsync(CancellationToken ct = default);
    Task<TemplateResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TemplateResponseDto> CreateAsync(CreateTemplateDto dto, CancellationToken ct = default);
    Task<TemplateResponseDto?> UpdateAsync(Guid id, UpdateTemplateDto dto, CancellationToken ct = default);
}
