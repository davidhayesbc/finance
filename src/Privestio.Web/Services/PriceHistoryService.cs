using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IPriceHistoryService
{
    Task<IReadOnlyList<PriceHistoryResponse>> GetBySymbolAsync(
        string symbol,
        DateOnly? fromDate = null,
        DateOnly? toDate = null
    );
    Task<PriceHistoryResponse?> UpdateAsync(Guid id, decimal price, string currency);
    Task<bool> DeleteAsync(Guid id);
    Task<HistoricalPriceSyncResponse?> SyncHistoricalPricesAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null
    );
}

public class PriceHistoryService : IPriceHistoryService
{
    private readonly HttpClient _httpClient;

    public PriceHistoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PriceHistoryResponse>> GetBySymbolAsync(
        string symbol,
        DateOnly? fromDate = null,
        DateOnly? toDate = null
    )
    {
        try
        {
            var parameters = new List<string>();
            if (fromDate.HasValue)
                parameters.Add($"fromDate={fromDate:yyyy-MM-dd}");
            if (toDate.HasValue)
                parameters.Add($"toDate={toDate:yyyy-MM-dd}");

            var query = parameters.Count == 0 ? string.Empty : $"?{string.Join("&", parameters)}";
            var result = await _httpClient.GetFromJsonAsync<List<PriceHistoryResponse>>(
                $"/api/v1/price-history/{Uri.EscapeDataString(symbol)}{query}"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<PriceHistoryResponse?> UpdateAsync(Guid id, decimal price, string currency)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/price-history/{id}",
                new UpdatePriceHistoryRequest { Price = price, Currency = currency }
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<PriceHistoryResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/price-history/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HistoricalPriceSyncResponse?> SyncHistoricalPricesAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null
    )
    {
        try
        {
            var parameters = new List<string>();
            if (fromDate.HasValue)
                parameters.Add($"fromDate={fromDate:yyyy-MM-dd}");
            if (toDate.HasValue)
                parameters.Add($"toDate={toDate:yyyy-MM-dd}");

            var query = parameters.Count == 0 ? string.Empty : $"?{string.Join("&", parameters)}";
            var response = await _httpClient.PostAsync(
                $"/api/v1/price-history/sync/historical{query}",
                null
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<HistoricalPriceSyncResponse>();
        }
        catch
        {
            return null;
        }
    }
}
