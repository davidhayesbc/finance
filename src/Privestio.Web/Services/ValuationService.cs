using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IValuationService
{
    Task<IReadOnlyList<ValuationResponse>> GetValuationsAsync(Guid accountId);
}

public class ValuationService : IValuationService
{
    private readonly HttpClient _httpClient;

    public ValuationService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<ValuationResponse>> GetValuationsAsync(Guid accountId)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<ValuationResponse>>(
                $"/api/v1/accounts/{accountId}/valuations"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }
}
