using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;
using ComponentesIA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComponentesIA.Infrastructure.Repositories;

public class ExtractionJobRepository : IExtractionJobRepository
{
    private readonly AppDbContext _context;

    public ExtractionJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ExtractionJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.ExtractionJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<ExtractionJob?> GetByIdWithResultAsync(Guid id, CancellationToken ct = default)
        => _context.ExtractionJobs
            .Include(j => j.DocumentFile)
            .Include(j => j.Result)
                .ThenInclude(r => r!.FieldResults)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(ExtractionJob job, CancellationToken ct = default)
        => await _context.ExtractionJobs.AddAsync(job, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
