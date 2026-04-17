using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class SyncTombstoneRepository : ISyncTombstoneRepository
{
    private readonly PrivestioDbContext _context;

    public SyncTombstoneRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SyncTombstone>> GetUnsyncedAsync(
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .SyncTombstones.Where(t => t.SyncedAt == null)
            .OrderBy(t => t.DeletedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SyncTombstone>> GetSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .SyncTombstones.Where(t => t.DeletedAtUtc > since)
            .OrderBy(t => t.DeletedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SyncTombstone>> GetSinceForUserAsync(
        DateTime since,
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .SyncTombstones.Where(t => t.DeletedAtUtc > since && t.UserId == userId)
            .OrderBy(t => t.DeletedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<SyncTombstone> AddAsync(
        SyncTombstone tombstone,
        CancellationToken cancellationToken = default
    )
    {
        await _context.SyncTombstones.AddAsync(tombstone, cancellationToken);
        return tombstone;
    }

    public async Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tombstone = await _context.SyncTombstones.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        tombstone?.MarkSynced();
    }
}
