using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IPriceHistoryRepository
{
    Task<PriceHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceHistory>> GetBySecurityIdAsync(
        Guid securityId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default
    );
    Task<PriceHistory> AddAsync(
        PriceHistory priceHistory,
        CancellationToken cancellationToken = default
    );
    Task AddRangeAsync(
        IEnumerable<PriceHistory> priceHistories,
        CancellationToken cancellationToken = default
    );
    Task<bool> ExistsBySecurityIdAndDateAsync(
        Guid securityId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlySet<(Guid SecurityId, DateOnly AsOfDate)>> GetExistingKeysAsync(
        IEnumerable<(Guid SecurityId, DateOnly AsOfDate)> keys,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the most recent <see cref="PriceHistory"/> entry per security for the given set of
    /// security ids. Securities with no recorded price are omitted from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PriceHistory>> GetLatestBySecurityIdsAsync(
        IEnumerable<Guid> securityIds,
        CancellationToken cancellationToken = default
    );
}
