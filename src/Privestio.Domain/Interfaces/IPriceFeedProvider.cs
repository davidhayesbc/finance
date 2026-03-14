namespace Privestio.Domain.Interfaces;

/// <summary>
/// A quote returned by a price feed provider for a given security symbol.
/// </summary>
public record PriceQuote(string Symbol, decimal Price, string Currency, DateOnly AsOfDate);

/// <summary>
/// Plugin interface for market price feed providers (e.g. Yahoo Finance, Alpha Vantage).
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface IPriceFeedProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<PriceQuote>> GetLatestPricesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PriceQuote>> GetHistoricalPricesAsync(
        string symbol,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    );
}
