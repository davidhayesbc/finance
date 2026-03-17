using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IPriceHistoryService
{
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
