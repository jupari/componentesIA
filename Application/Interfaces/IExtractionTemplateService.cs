using ComponentesIA.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace ComponentesIA.Application.Interfaces;

public interface IExtractionTemplateService
{
    Task<List<TemplateResponseDto>> GetAllAsync(CancellationToken ct = default);
    Task<TemplateResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TemplateResponseDto> CreateAsync(CreateTemplateDto dto, CancellationToken ct = default);
    Task<TemplateResponseDto?> UpdateAsync(Guid id, UpdateTemplateDto dto, CancellationToken ct = default);
    Task<TemplateResponseDto?> UploadSampleAsync(Guid id, IFormFile file, CancellationToken ct = default);
    Task<(byte[] Bytes, string ContentType, string FileName)?> GetSampleAsync(Guid id, CancellationToken ct = default);
}
