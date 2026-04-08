using ComponentesIA.Domain.Entities;

namespace ComponentesIA.Application.Interfaces;

public interface IDocumentBatchRepository
{
    Task<List<DocumentBatch>> GetAllAsync(string? uploadedBy = null, CancellationToken ct = default);
    Task<DocumentBatch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DocumentBatch?> GetByIdWithJobsAsync(Guid id, CancellationToken ct = default);
    Task<DocumentBatch?> GetByIdWithJobsDetailAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(DocumentBatch batch, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
