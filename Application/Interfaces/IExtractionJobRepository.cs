using ComponentesIA.Domain.Entities;

namespace ComponentesIA.Application.Interfaces;

public interface IExtractionJobRepository
{
    Task<ExtractionJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExtractionJob?> GetByIdWithResultAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ExtractionJob job, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
