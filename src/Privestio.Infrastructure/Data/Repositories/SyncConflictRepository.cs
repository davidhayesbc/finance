using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class SyncConflictRepository : ISyncConflictRepository
{
    private readonly PrivestioDbContext _context;

    public SyncConflictRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<SyncConflict?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.SyncConflicts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SyncConflict>> GetPendingAsync(
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .SyncConflicts.Where(c => c.Status == "Pending")
            .OrderByDescending(c => c.DetectedAt)
            .ToListAsync(cancellationToken);

    public async Task<SyncConflict> AddAsync(
        SyncConflict conflict,
        CancellationToken cancellationToken = default
    )
    {
        await _context.SyncConflicts.AddAsync(conflict, cancellationToken);
        return conflict;
    }

    public async Task<SyncConflict> UpdateAsync(
        SyncConflict conflict,
        CancellationToken cancellationToken = default
    )
    {
        _context.SyncConflicts.Update(conflict);
        return await Task.FromResult(conflict);
    }
}
