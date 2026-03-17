using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IHoldingService
{
    Task<IReadOnlyList<HoldingResponse>> GetHoldingsAsync(Guid accountId);
    Task<IReadOnlyList<SecurityAliasResponse>> GetAliasesAsync(Guid holdingId);
    Task<SecurityAliasResponse?> AddAliasAsync(Guid holdingId, AddSecurityAliasRequest request);
    Task<bool> DeleteAliasAsync(Guid holdingId, Guid aliasId);
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

    public async Task<IReadOnlyList<SecurityAliasResponse>> GetAliasesAsync(Guid holdingId)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<SecurityAliasResponse>>(
                $"/api/v1/holdings/{holdingId}/aliases"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<SecurityAliasResponse?> AddAliasAsync(
        Guid holdingId,
        AddSecurityAliasRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/holdings/{holdingId}/aliases",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SecurityAliasResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAliasAsync(Guid holdingId, Guid aliasId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/v1/holdings/{holdingId}/aliases/{aliasId}"
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
