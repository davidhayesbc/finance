using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IPriceHistoryRepository
{
    Task<PriceHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceHistory>> GetBySymbolAsync(
        string symbol,
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
    Task<bool> ExistsBySymbolAndDateAsync(
        string symbol,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlySet<(string Symbol, DateOnly AsOfDate)>> GetExistingKeysAsync(
        IEnumerable<(string Symbol, DateOnly AsOfDate)> keys,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the most recent <see cref="PriceHistory"/> entry per symbol for the given set of
    /// symbols. Symbols with no recorded price are omitted from the result.
    /// </summary>
    Task<IReadOnlyDictionary<string, PriceHistory>> GetLatestBySymbolsAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default
    );
}
