using Microsoft.Extensions.Logging;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.PriceFeeds;

/// <summary>
/// Composite price feed provider that tries multiple providers in a configurable order.
/// Each lookup can carry per-provider symbol mappings via <see cref="PriceLookup.ProviderSymbols"/>.
/// </summary>
public class ChainedPriceFeedProvider : IPriceFeedProvider
{
    private readonly IReadOnlyDictionary<string, IPriceFeedProvider> _providersByName;
    private readonly IReadOnlyList<string> _defaultOrder;
    private readonly ILogger<ChainedPriceFeedProvider> _logger;

    public string ProviderName => "Chained";

    public ChainedPriceFeedProvider(
        IReadOnlyDictionary<string, IPriceFeedProvider> providersByName,
        IReadOnlyList<string> defaultOrder,
        ILogger<ChainedPriceFeedProvider> logger
    )
    {
        _providersByName = providersByName;
        _defaultOrder = defaultOrder;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PriceQuote>> GetLatestPricesAsync(
        IEnumerable<PriceLookup> lookups,
        CancellationToken cancellationToken = default
    )
    {
        var lookupList = lookups.ToList();
        var allResults = new List<PriceQuote>();
        var resolved = new HashSet<Guid>();

        foreach (var providerName in _defaultOrder)
        {
            if (!_providersByName.TryGetValue(providerName, out var provider))
                continue;

            var unresolved = lookupList.Where(l => !resolved.Contains(l.SecurityId)).ToList();
            if (unresolved.Count == 0)
                break;

            var remapped = unresolved.Select(l => RemapLookup(l, providerName)).ToList();

            var results = await provider.GetLatestPricesAsync(remapped, cancellationToken);

            foreach (var quote in results)
            {
                resolved.Add(quote.SecurityId);
                allResults.Add(quote);
            }

            if (results.Count < unresolved.Count)
            {
                _logger.LogDebug(
                    "Provider {Provider} returned prices for {Resolved}/{Total} securities",
                    providerName,
                    results.Count,
                    unresolved.Count
                );
            }
        }

        return allResults.AsReadOnly();
    }

    public async Task<IReadOnlyList<PriceQuote>> GetHistoricalPricesAsync(
        PriceLookup lookup,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var providerName in _defaultOrder)
        {
            if (!_providersByName.TryGetValue(providerName, out var provider))
                continue;

            var remapped = RemapLookup(lookup, providerName);

            var results = await provider.GetHistoricalPricesAsync(
                remapped,
                fromDate,
                toDate,
                cancellationToken
            );

            if (results.Count > 0)
                return results;

            _logger.LogDebug(
                "Provider {Provider} returned no historical prices for {SecurityId}, trying next",
                providerName,
                lookup.SecurityId
            );
        }

        return Array.Empty<PriceQuote>();
    }

    private static PriceLookup RemapLookup(PriceLookup lookup, string providerName)
    {
        if (
            lookup.ProviderSymbols is not null
            && lookup.ProviderSymbols.TryGetValue(providerName, out var providerSymbol)
        )
        {
            return lookup with { Symbol = providerSymbol };
        }

        return lookup;
    }
}
