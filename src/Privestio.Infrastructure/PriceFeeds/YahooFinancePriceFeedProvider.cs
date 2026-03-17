using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.PriceFeeds;

/// <summary>
/// Price feed provider using the unofficial Yahoo Finance v8 chart API.
/// No API key required; intended for personal self-hosted use.
/// </summary>
public class YahooFinancePriceFeedProvider : IPriceFeedProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinancePriceFeedProvider> _logger;

    public string ProviderName => "YahooFinance";

    public YahooFinancePriceFeedProvider(
        HttpClient httpClient,
        ILogger<YahooFinancePriceFeedProvider> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PriceQuote>> GetLatestPricesAsync(
        IEnumerable<PriceLookup> lookups,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<PriceQuote>();
        foreach (var lookup in lookups)
        {
            var quote = await FetchLatestAsync(lookup, cancellationToken);
            if (quote is not null)
                results.Add(quote);
        }
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<PriceQuote>> GetHistoricalPricesAsync(
        PriceLookup lookup,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    )
    {
        var period1 = new DateTimeOffset(
            fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        ).ToUnixTimeSeconds();
        var period2 = new DateTimeOffset(
            toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
        ).ToUnixTimeSeconds();

        var url =
            $"v8/finance/chart/{Uri.EscapeDataString(lookup.Symbol)}?interval=1d&period1={period1}&period2={period2}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<YahooChartResponse>(
                url,
                cancellationToken
            );
            return ParseHistoricalPrices(lookup, response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch historical prices for security {SecurityId} using {LookupSymbol}",
                lookup.SecurityId,
                lookup.Symbol
            );
            return [];
        }
    }

    private async Task<PriceQuote?> FetchLatestAsync(
        PriceLookup lookup,
        CancellationToken cancellationToken
    )
    {
        var url = $"v8/finance/chart/{Uri.EscapeDataString(lookup.Symbol)}?interval=1d&range=1d";
        try
        {
            var response = await _httpClient.GetFromJsonAsync<YahooChartResponse>(
                url,
                cancellationToken
            );
            var meta = response?.Chart?.Result?.FirstOrDefault()?.Meta;
            if (meta is null || meta.RegularMarketPrice <= 0)
                return null;

            return new PriceQuote(
                lookup.SecurityId,
                lookup.Symbol,
                Math.Round((decimal)meta.RegularMarketPrice, 6, MidpointRounding.ToEven),
                (meta.Currency ?? "USD").ToUpperInvariant(),
                DateOnly.FromDateTime(DateTime.UtcNow)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch latest price for security {SecurityId} using {LookupSymbol}",
                lookup.SecurityId,
                lookup.Symbol
            );
            return null;
        }
    }

    private static IReadOnlyList<PriceQuote> ParseHistoricalPrices(
        PriceLookup lookup,
        YahooChartResponse? response
    )
    {
        var chartResult = response?.Chart?.Result?.FirstOrDefault();
        if (chartResult is null)
            return [];

        var timestamps = chartResult.Timestamps ?? [];
        var closes = chartResult.Indicators?.Quote?.FirstOrDefault()?.Close ?? [];
        var currency = (chartResult.Meta?.Currency ?? "USD").ToUpperInvariant();

        var prices = new List<PriceQuote>();
        for (var i = 0; i < Math.Min(timestamps.Count, closes.Count); i++)
        {
            if (closes[i] is not { } closePrice || closePrice <= 0)
                continue;

            var date = DateOnly.FromDateTime(
                DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime
            );
            prices.Add(
                new PriceQuote(
                    lookup.SecurityId,
                    lookup.Symbol,
                    Math.Round((decimal)closePrice, 6, MidpointRounding.ToEven),
                    currency,
                    date
                )
            );
        }
        return prices.AsReadOnly();
    }

    // ── Private JSON DTOs ────────────────────────────────────────────────────

    private sealed class YahooChartResponse
    {
        [JsonPropertyName("chart")]
        public YahooChart? Chart { get; init; }
    }

    private sealed class YahooChart
    {
        [JsonPropertyName("result")]
        public List<YahooChartResult>? Result { get; init; }
    }

    private sealed class YahooChartResult
    {
        [JsonPropertyName("meta")]
        public YahooMeta? Meta { get; init; }

        [JsonPropertyName("timestamp")]
        public List<long>? Timestamps { get; init; }

        [JsonPropertyName("indicators")]
        public YahooIndicators? Indicators { get; init; }
    }

    private sealed class YahooMeta
    {
        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("regularMarketPrice")]
        public double RegularMarketPrice { get; init; }
    }

    private sealed class YahooIndicators
    {
        [JsonPropertyName("quote")]
        public List<YahooQuoteData>? Quote { get; init; }
    }

    private sealed class YahooQuoteData
    {
        [JsonPropertyName("close")]
        public List<double?>? Close { get; init; }
    }
}
