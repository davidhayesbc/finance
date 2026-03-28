using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.PriceFeeds;

/// <summary>
/// Price feed provider using the MSN Finance / Bing Finance APIs.
/// Resolves ticker symbols to MSN instrument IDs via the Bing autosuggest
/// endpoint, then fetches quotes and historical chart data.
/// </summary>
public class MsnFinancePriceFeedProvider : IPriceSourcePlugin
{
    private const string ApiKey = "0QfOX3Vn51YCzitbLaRkTTBadtWpgTN8NZLW0C1SEM";
    private const string CommonParams = $"apikey={ApiKey}&cm=en-ca&it=web&wrapodata=false";

    private const string AutosuggestBaseUrl =
        "https://services.bingapis.com/contentservices-finance.csautosuggest/api/v1/Query";

    private readonly HttpClient _httpClient;
    private readonly ILogger<MsnFinancePriceFeedProvider> _logger;
    private readonly ConcurrentDictionary<string, string> _instrumentIdCache = new();

    public string ProviderName => "MsnFinance";

    public MsnFinancePriceFeedProvider(
        HttpClient httpClient,
        ILogger<MsnFinancePriceFeedProvider> logger
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
        var resolvedLookups = new List<(PriceLookup Lookup, string InstrumentId)>();
        foreach (var lookup in lookups)
        {
            var instrumentId = await ResolveInstrumentIdAsync(lookup.Symbol, cancellationToken);
            if (instrumentId is not null)
                resolvedLookups.Add((lookup, instrumentId));
        }

        if (resolvedLookups.Count == 0)
            return [];

        var ids = string.Join(",", resolvedLookups.Select(r => r.InstrumentId));
        var url = $"Quotes?ids={ids}&{CommonParams}";

        try
        {
            var quotes = await _httpClient.GetFromJsonAsync<List<MsnQuoteResponse>>(
                url,
                cancellationToken
            );
            if (quotes is null)
                return [];

            var results = new List<PriceQuote>();
            foreach (var (lookup, instrumentId) in resolvedLookups)
            {
                var quote = quotes.FirstOrDefault(q =>
                    string.Equals(q.InstrumentId, instrumentId, StringComparison.OrdinalIgnoreCase)
                );
                if (quote is null || quote.Price <= 0)
                    continue;

                var asOfDate =
                    quote.TimeLastTraded is not null
                    && DateTimeOffset.TryParse(quote.TimeLastTraded, out var dto)
                        ? DateOnly.FromDateTime(dto.UtcDateTime)
                        : DateOnly.FromDateTime(DateTime.UtcNow);

                results.Add(
                    new PriceQuote(
                        lookup.SecurityId,
                        lookup.Symbol,
                        Math.Round((decimal)quote.Price, 6, MidpointRounding.ToEven),
                        (quote.Currency ?? "USD").ToUpperInvariant(),
                        asOfDate,
                        Source: "MsnFinance"
                    )
                );
            }

            return results.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch latest prices from MSN Finance for {Count} symbols",
                resolvedLookups.Count
            );
            return [];
        }
    }

    public async Task<IReadOnlyList<PriceQuote>> GetHistoricalPricesAsync(
        PriceLookup lookup,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    )
    {
        var instrumentId = await ResolveInstrumentIdAsync(lookup.Symbol, cancellationToken);
        if (instrumentId is null)
        {
            _logger.LogWarning(
                "Could not resolve MSN instrument ID for security {SecurityId} symbol {Symbol}",
                lookup.SecurityId,
                lookup.Symbol
            );
            return [];
        }

        var daySpan = toDate.DayNumber - fromDate.DayNumber;
        var chartType = daySpan > 365 ? "5Y" : "1Y";
        var url = $"Charts?ids={instrumentId}&type={chartType}&{CommonParams}";

        try
        {
            var charts = await _httpClient.GetFromJsonAsync<List<MsnChartResponse>>(
                url,
                cancellationToken
            );
            var chart = charts?.FirstOrDefault();
            if (chart?.Series is null)
                return [];

            var timestamps = chart.Series.TimeStamps ?? [];
            var prices = chart.Series.Prices ?? [];

            var results = new List<PriceQuote>();
            for (var i = 0; i < Math.Min(timestamps.Count, prices.Count); i++)
            {
                if (!DateTimeOffset.TryParse(timestamps[i], out var tsOffset))
                    continue;

                var date = DateOnly.FromDateTime(tsOffset.UtcDateTime);
                if (date < fromDate || date > toDate)
                    continue;

                if (prices[i] is not { } price || price <= 0)
                    continue;

                results.Add(
                    new PriceQuote(
                        lookup.SecurityId,
                        lookup.Symbol,
                        Math.Round((decimal)price, 6, MidpointRounding.ToEven),
                        chart.Currency?.ToUpperInvariant() ?? "USD",
                        date,
                        Source: "MsnFinance"
                    )
                );
            }

            return results.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch historical prices for security {SecurityId} symbol {Symbol}",
                lookup.SecurityId,
                lookup.Symbol
            );
            return [];
        }
    }

    private async Task<string?> ResolveInstrumentIdAsync(
        string symbol,
        CancellationToken cancellationToken
    )
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        if (_instrumentIdCache.TryGetValue(normalizedSymbol, out var cached))
            return cached;

        // Autosuggest lives on a different host; use an absolute URI so the
        // HttpClient's BaseAddress (assets.msn.com) is bypassed.
        var url =
            $"{AutosuggestBaseUrl}?query={Uri.EscapeDataString(normalizedSymbol)}&market=en-ca&top=5";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<MsnAutosuggestResponse>(
                new Uri(url),
                cancellationToken
            );

            if (response?.Data?.Stocks is null || response.Data.Stocks.Count == 0)
            {
                _logger.LogDebug("No autosuggest results for symbol {Symbol}", normalizedSymbol);
                return null;
            }

            // Try to find an exact ticker match first.
            foreach (var stockJson in response.Data.Stocks)
            {
                var stock = JsonSerializer.Deserialize<MsnStockEntry>(stockJson);
                if (stock?.SecId is null)
                    continue;

                var ticker = (stock.Ticker ?? stock.TickerAlt)?.Trim().ToUpperInvariant();
                if (string.Equals(ticker, normalizedSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    _instrumentIdCache.TryAdd(normalizedSymbol, stock.SecId);
                    return stock.SecId;
                }
            }

            // Fall back to the first result that has a SecId.
            var fallback = response
                .Data.Stocks.Select(s => JsonSerializer.Deserialize<MsnStockEntry>(s))
                .FirstOrDefault(s => s?.SecId is not null);

            if (fallback?.SecId is not null)
            {
                _instrumentIdCache.TryAdd(normalizedSymbol, fallback.SecId);
                return fallback.SecId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to resolve MSN instrument ID for symbol {Symbol}",
                normalizedSymbol
            );
            return null;
        }
    }

    // Private JSON DTOs

    private sealed class MsnQuoteResponse
    {
        [JsonPropertyName("price")]
        public double Price { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("instrumentId")]
        public string? InstrumentId { get; init; }

        [JsonPropertyName("timeLastTraded")]
        public string? TimeLastTraded { get; init; }
    }

    private sealed class MsnChartResponse
    {
        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("series")]
        public MsnChartSeries? Series { get; init; }
    }

    private sealed class MsnChartSeries
    {
        [JsonPropertyName("timeStamps")]
        public List<string>? TimeStamps { get; init; }

        [JsonPropertyName("prices")]
        public List<double?>? Prices { get; init; }
    }

    private sealed class MsnAutosuggestResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; init; }

        [JsonPropertyName("data")]
        public MsnAutosuggestData? Data { get; init; }
    }

    private sealed class MsnAutosuggestData
    {
        [JsonPropertyName("stocks")]
        public List<string>? Stocks { get; init; }
    }

    private sealed class MsnStockEntry
    {
        [JsonPropertyName("SecId")]
        public string? SecId { get; init; }

        [JsonPropertyName("RT00S")]
        public string? Ticker { get; init; }

        [JsonPropertyName("OS001")]
        public string? TickerAlt { get; init; }

        [JsonPropertyName("RT0SN")]
        public string? Name { get; init; }

        [JsonPropertyName("OS01W")]
        public string? NameAlt { get; init; }
    }
}
