using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Infrastructure.Repositories;

public class ExtractionTemplateRepository : IExtractionTemplateRepository
{
    private readonly AppDbContext _context;

    public ExtractionTemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ExtractionTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.ExtractionTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<ExtractionTemplate?> GetByIdWithFieldsAsync(Guid id, CancellationToken ct = default)
        => _context.ExtractionTemplates
            .Include(t => t.Fields.OrderBy(f => f.Order))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<ExtractionTemplate>> GetAllActiveAsync(CancellationToken ct = default)
        => _context.ExtractionTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.ExtractionTemplates.Where(t => t.Code == code);
        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(ExtractionTemplate template, CancellationToken ct = default)
        => await _context.ExtractionTemplates.AddAsync(template, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
