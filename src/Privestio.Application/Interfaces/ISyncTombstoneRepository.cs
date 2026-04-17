using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ISyncTombstoneRepository
{
    Task<IReadOnlyList<SyncTombstone>> GetUnsyncedAsync(
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<SyncTombstone>> GetSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<SyncTombstone>> GetSinceForUserAsync(
        DateTime since,
        Guid userId,
        CancellationToken cancellationToken = default
    );
    Task<SyncTombstone> AddAsync(
        SyncTombstone tombstone,
        CancellationToken cancellationToken = default
    );
    Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default);
}
