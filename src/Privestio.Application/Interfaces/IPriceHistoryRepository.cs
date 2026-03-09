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
}
