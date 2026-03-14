using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IValuationService
{
    Task<IReadOnlyList<ValuationResponse>> GetValuationsAsync(Guid accountId);
    Task<ValuationResponse?> CreateValuationAsync(Guid accountId, CreateValuationRequest request);
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

    public async Task<ValuationResponse?> CreateValuationAsync(
        Guid accountId,
        CreateValuationRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/accounts/{accountId}/valuations",
                request
            );

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ValuationResponse>();
        }
        catch
        {
            return null;
        }
    }
}
