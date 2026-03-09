using Privestio.Domain.Entities;

namespace Privestio.Application.Interfaces;

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExchangeRate?> GetLatestByPairAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default
    );
    Task<ExchangeRate> AddAsync(
        ExchangeRate exchangeRate,
        CancellationToken cancellationToken = default
    );
    Task AddRangeAsync(
        IEnumerable<ExchangeRate> exchangeRates,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<ExchangeRate>> GetAllAsync(
        string? fromCurrency = null,
        string? toCurrency = null,
        CancellationToken cancellationToken = default
    );
}
