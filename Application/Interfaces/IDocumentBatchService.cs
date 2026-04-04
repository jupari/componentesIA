using ComponentesIA.Application.DTOs;

namespace ComponentesIA.Application.Interfaces;

public interface IDocumentBatchService
{
    Task<BatchResponseDto> CreateBatchAsync(CreateBatchDto dto, IReadOnlyList<IFormFile> files, CancellationToken ct = default);
    Task<BatchResponseDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<List<JobSummaryDto>> GetBatchJobsAsync(Guid batchId, CancellationToken ct = default);
}
