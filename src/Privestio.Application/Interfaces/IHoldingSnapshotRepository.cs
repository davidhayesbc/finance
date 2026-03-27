using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IHoldingSnapshotRepository
{
    Task<IReadOnlyList<HoldingSnapshot>> GetByAccountIdAsync(
        Guid accountId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlySet<(Guid SecurityId, DateOnly AsOfDate)>> GetExistingKeysAsync(
        Guid accountId,
        IEnumerable<(Guid SecurityId, DateOnly AsOfDate)> keys,
        CancellationToken cancellationToken = default
    );

    Task AddRangeAsync(
        IEnumerable<HoldingSnapshot> snapshots,
        CancellationToken cancellationToken = default
    );

    Task DeleteByAccountIdAndDateAsync(
        Guid accountId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default
    );
}
