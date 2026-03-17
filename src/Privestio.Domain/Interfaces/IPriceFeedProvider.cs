namespace Privestio.Domain.Interfaces;

/// <summary>
/// A lookup request for a provider-specific market symbol.
/// </summary>
public record PriceLookup(Guid SecurityId, string Symbol);

/// <summary>
/// A quote returned by a price feed provider for a given security symbol.
/// </summary>
public record PriceQuote(
    Guid SecurityId,
    string Symbol,
    decimal Price,
    string Currency,
    DateOnly AsOfDate,
    string? Source = null
);

/// <summary>
/// Plugin interface for market price feed providers (e.g. Yahoo Finance, Alpha Vantage).
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface IPriceFeedProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<PriceQuote>> GetLatestPricesAsync(
        IEnumerable<PriceLookup> lookups,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PriceQuote>> GetHistoricalPricesAsync(
        PriceLookup lookup,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    );
}
