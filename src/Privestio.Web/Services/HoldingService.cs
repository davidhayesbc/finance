using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IHoldingService
{
    Task<IReadOnlyList<HoldingResponse>> GetHoldingsAsync(Guid accountId);
}

public class HoldingService : IHoldingService
{
    private readonly HttpClient _httpClient;

    public HoldingService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<HoldingResponse>> GetHoldingsAsync(Guid accountId)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<HoldingResponse>>(
                $"/api/v1/accounts/{accountId}/holdings"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }
}
