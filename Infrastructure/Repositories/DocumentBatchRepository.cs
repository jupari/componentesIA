using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Infrastructure.Repositories;

public class DocumentBatchRepository : IDocumentBatchRepository
{
    private readonly AppDbContext _context;

    public DocumentBatchRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<DocumentBatch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.DocumentBatches.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<DocumentBatch?> GetByIdWithJobsAsync(Guid id, CancellationToken ct = default)
        => _context.DocumentBatches
            .Include(b => b.Documents)
                .ThenInclude(d => d.Job)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(DocumentBatch batch, CancellationToken ct = default)
        => await _context.DocumentBatches.AddAsync(batch, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
