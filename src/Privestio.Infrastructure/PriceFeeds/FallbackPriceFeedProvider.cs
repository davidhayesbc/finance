using Microsoft.Extensions.Logging;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.PriceFeeds;

/// <summary>
/// Composite price feed provider that tries a primary provider first and falls
/// back to a secondary provider for securities the primary cannot price.
/// </summary>
public class FallbackPriceFeedProvider : IPriceFeedProvider
{
    private readonly IPriceFeedProvider _primary;
    private readonly IPriceFeedProvider _fallback;
    private readonly ILogger<FallbackPriceFeedProvider> _logger;

    public string ProviderName => "FallbackComposite";

    public FallbackPriceFeedProvider(
        IPriceFeedProvider primary,
        IPriceFeedProvider fallback,
        ILogger<FallbackPriceFeedProvider> logger
    )
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PriceQuote>> GetLatestPricesAsync(
        IEnumerable<PriceLookup> lookups,
        CancellationToken cancellationToken = default
    )
    {
        var lookupList = lookups.ToList();
        var primaryResults = await _primary.GetLatestPricesAsync(lookupList, cancellationToken);

        var resolved = primaryResults.Select(q => q.SecurityId).ToHashSet();
        var unresolved = lookupList.Where(l => !resolved.Contains(l.SecurityId)).ToList();

        if (unresolved.Count == 0)
            return primaryResults;

        _logger.LogDebug(
            "Primary provider {Primary} returned no price for {Count} securities, trying fallback {Fallback}",
            _primary.ProviderName,
            unresolved.Count,
            _fallback.ProviderName
        );

        var fallbackResults = await _fallback.GetLatestPricesAsync(unresolved, cancellationToken);

        return primaryResults.Concat(fallbackResults).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<PriceQuote>> GetHistoricalPricesAsync(
        PriceLookup lookup,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    )
    {
        var primaryResults = await _primary.GetHistoricalPricesAsync(
            lookup,
            fromDate,
            toDate,
            cancellationToken
        );

        if (primaryResults.Count > 0)
            return primaryResults;

        _logger.LogDebug(
            "Primary provider {Primary} returned no historical prices for {SecurityId}, trying fallback {Fallback}",
            _primary.ProviderName,
            lookup.SecurityId,
            _fallback.ProviderName
        );

        return await _fallback.GetHistoricalPricesAsync(
            lookup,
            fromDate,
            toDate,
            cancellationToken
        );
    }
}
