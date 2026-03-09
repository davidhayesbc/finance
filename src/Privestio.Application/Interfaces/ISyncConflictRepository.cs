using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface ISyncConflictRepository
{
    Task<SyncConflict?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SyncConflict>> GetPendingAsync(
        CancellationToken cancellationToken = default
    );
    Task<SyncConflict> AddAsync(
        SyncConflict conflict,
        CancellationToken cancellationToken = default
    );
    Task<SyncConflict> UpdateAsync(
        SyncConflict conflict,
        CancellationToken cancellationToken = default
    );
}
