using ComponentesIA.Domain.Entities;

namespace ComponentesIA.Application.Interfaces;

public interface IExtractionTemplateRepository
{
    Task<ExtractionTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExtractionTemplate?> GetByIdWithFieldsAsync(Guid id, CancellationToken ct = default);
    Task<List<ExtractionTemplate>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
    void RemoveFields(IEnumerable<ExtractionField> fields);
    void AddFields(IEnumerable<ExtractionField> fields);
    Task AddAsync(ExtractionTemplate template, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
