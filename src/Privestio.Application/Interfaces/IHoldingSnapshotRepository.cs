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

    /// <summary>
    /// Returns the sum of the most recent <see cref="HoldingSnapshot.MarketValue"/> per security
    /// for the given account, or <c>null</c> if no snapshots exist.
    /// Use this as the authoritative current balance for snapshot-based investment accounts
    /// (e.g. managed funds imported from PDF statements).
    /// </summary>
    Task<decimal?> GetCurrentSnapshotTotalAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    );
}
