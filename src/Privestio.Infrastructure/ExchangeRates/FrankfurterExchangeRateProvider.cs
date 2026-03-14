using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.ExchangeRates;

/// <summary>
/// Exchange rate ingestion provider using the free, open-source Frankfurter API
/// (<c>https://api.frankfurter.app</c>). No API key required; data is sourced from the
/// European Central Bank reference rates.
/// </summary>
public class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterExchangeRateProvider> _logger;

    public string ProviderName => "Frankfurter";

    public FrankfurterExchangeRateProvider(
        HttpClient httpClient,
        ILogger<FrankfurterExchangeRateProvider> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExchangeRateQuote>> GetLatestRatesAsync(
        string baseCurrency,
        IEnumerable<string> targetCurrencies,
        CancellationToken cancellationToken = default
    )
    {
        var targets = string.Join(",", targetCurrencies.Select(t => t.ToUpperInvariant().Trim()));
        var url =
            $"latest?from={Uri.EscapeDataString(baseCurrency.ToUpperInvariant().Trim())}&to={targets}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<FrankfurterResponse>(
                url,
                cancellationToken
            );
            return ParseRates(response, baseCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch latest exchange rates from {Provider} for base {BaseCurrency}",
                ProviderName,
                baseCurrency
            );
            return [];
        }
    }

    public async Task<IReadOnlyList<ExchangeRateQuote>> GetHistoricalRatesAsync(
        string baseCurrency,
        IEnumerable<string> targetCurrencies,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default
    )
    {
        var targets = string.Join(",", targetCurrencies.Select(t => t.ToUpperInvariant().Trim()));
        var base_ = Uri.EscapeDataString(baseCurrency.ToUpperInvariant().Trim());
        var url = $"{fromDate:yyyy-MM-dd}..{toDate:yyyy-MM-dd}?from={base_}&to={targets}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<FrankfurterTimeseriesResponse>(
                url,
                cancellationToken
            );
            return ParseTimeseriesRates(response, baseCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch historical exchange rates from {Provider} for base {BaseCurrency}",
                ProviderName,
                baseCurrency
            );
            return [];
        }
    }

    private static IReadOnlyList<ExchangeRateQuote> ParseRates(
        FrankfurterResponse? response,
        string baseCurrency
    )
    {
        if (response?.Rates is null || !DateOnly.TryParse(response.Date, out var date))
            return [];

        return response
            .Rates.Select(kvp => new ExchangeRateQuote(
                baseCurrency.ToUpperInvariant().Trim(),
                kvp.Key.ToUpperInvariant(),
                Math.Round((decimal)kvp.Value, 6, MidpointRounding.ToEven),
                date
            ))
            .ToList()
            .AsReadOnly();
    }

    private static IReadOnlyList<ExchangeRateQuote> ParseTimeseriesRates(
        FrankfurterTimeseriesResponse? response,
        string baseCurrency
    )
    {
        if (response?.Rates is null)
            return [];

        var quotes = new List<ExchangeRateQuote>();
        foreach (var (dateStr, ratePairs) in response.Rates)
        {
            if (!DateOnly.TryParse(dateStr, out var date))
                continue;

            foreach (var (currency, rate) in ratePairs)
            {
                quotes.Add(
                    new ExchangeRateQuote(
                        baseCurrency.ToUpperInvariant().Trim(),
                        currency.ToUpperInvariant(),
                        Math.Round((decimal)rate, 6, MidpointRounding.ToEven),
                        date
                    )
                );
            }
        }
        return quotes.AsReadOnly();
    }

    // ── Private JSON DTOs ────────────────────────────────────────────────────

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("base")]
        public string? Base { get; init; }

        [JsonPropertyName("rates")]
        public Dictionary<string, double>? Rates { get; init; }
    }

    private sealed class FrankfurterTimeseriesResponse
    {
        [JsonPropertyName("base")]
        public string? Base { get; init; }

        [JsonPropertyName("rates")]
        public Dictionary<string, Dictionary<string, double>>? Rates { get; init; }
    }
}
