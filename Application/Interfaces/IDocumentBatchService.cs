using ComponentesIA.Application.DTOs;

namespace ComponentesIA.Application.Interfaces;

public interface IDocumentBatchService
{
    Task<List<BatchResponseDto>> GetBatchesAsync(string? uploadedBy = null, CancellationToken ct = default);
    Task<BatchResponseDto> CreateBatchAsync(CreateBatchDto dto, IReadOnlyList<IFormFile> files, CancellationToken ct = default);
    Task<BatchResponseDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<List<JobSummaryDto>> GetBatchJobsAsync(Guid batchId, CancellationToken ct = default);
    Task<List<JobDetailDto>?> GetBatchJobsDetailAsync(Guid batchId, CancellationToken ct = default);
}
