namespace Privestio.Domain.Interfaces;

/// <summary>
/// A foreign exchange rate quote from a provider on a specific date.
/// </summary>
public record ExchangeRateQuote(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly AsOfDate
);

/// <summary>
/// Plugin interface for exchange rate ingestion providers (e.g. Frankfurter, ECB).
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface IExchangeRateProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<ExchangeRateQuote>> GetLatestRatesAsync(
        string baseCurrency,
        IEnumerable<string> targetCurrencies,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ExchangeRateQuote>> GetHistoricalRatesAsync(
        string baseCurrency,
        IEnumerable<string> targetCurrencies,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    );
}
