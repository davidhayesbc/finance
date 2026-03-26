using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface ISecurityWebService
{
    Task<IReadOnlyList<SecurityCatalogItemResponse>> GetSecuritiesAsync();
    Task<SecurityCatalogItemResponse?> UpdateSecurityAsync(
        Guid securityId,
        UpdateSecurityDetailsRequest request
    );
    Task<IReadOnlyList<SecurityConflictResponse>> GetConflictsAsync();
    Task<SecurityAliasResponse?> AddSecurityAliasAsync(
        Guid securityId,
        AddSecurityAliasRequest request
    );
    Task<SecurityAliasResponse?> UpdateSecurityAliasAsync(
        Guid securityId,
        Guid aliasId,
        AddSecurityAliasRequest request
    );
    Task<bool> DeleteSecurityAliasAsync(Guid securityId, Guid aliasId);
    Task<SecurityCatalogItemResponse?> SetPricingProviderOrderAsync(
        Guid securityId,
        List<string>? providerOrder
    );
    Task<IReadOnlyList<SecurityIdentifierResponse>> GetHoldingIdentifiersAsync(Guid holdingId);
    Task<SecurityIdentifierResponse?> AddHoldingIdentifierAsync(
        Guid holdingId,
        AddSecurityIdentifierRequest request
    );
    Task<bool> DeleteHoldingIdentifierAsync(Guid holdingId, Guid identifierId);
    Task<HoldingResponse?> CorrectHoldingSecurityAsync(
        Guid holdingId,
        CorrectHoldingSecurityRequest request
    );
    Task<SecurityCatalogItemResponse?> FetchPriceAsync(Guid securityId);
}

public class SecurityWebService : ISecurityWebService
{
    private readonly HttpClient _httpClient;

    public SecurityWebService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<SecurityCatalogItemResponse>> GetSecuritiesAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<SecurityCatalogItemResponse>>(
                "/api/v1/securities"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<SecurityCatalogItemResponse?> UpdateSecurityAsync(
        Guid securityId,
        UpdateSecurityDetailsRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/securities/{securityId}",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SecurityCatalogItemResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<SecurityConflictResponse>> GetConflictsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<SecurityConflictResponse>>(
                "/api/v1/securities/conflicts"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<SecurityAliasResponse?> AddSecurityAliasAsync(
        Guid securityId,
        AddSecurityAliasRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/securities/{securityId}/aliases",
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

    public async Task<SecurityAliasResponse?> UpdateSecurityAliasAsync(
        Guid securityId,
        Guid aliasId,
        AddSecurityAliasRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/securities/{securityId}/aliases/{aliasId}",
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

    public async Task<bool> DeleteSecurityAliasAsync(Guid securityId, Guid aliasId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/v1/securities/{securityId}/aliases/{aliasId}"
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<SecurityCatalogItemResponse?> SetPricingProviderOrderAsync(
        Guid securityId,
        List<string>? providerOrder
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/securities/{securityId}/pricing-order",
                new SetPricingProviderOrderRequest { ProviderOrder = providerOrder }
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SecurityCatalogItemResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<SecurityIdentifierResponse>> GetHoldingIdentifiersAsync(
        Guid holdingId
    )
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<SecurityIdentifierResponse>>(
                $"/api/v1/securities/holdings/{holdingId}/identifiers"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<SecurityIdentifierResponse?> AddHoldingIdentifierAsync(
        Guid holdingId,
        AddSecurityIdentifierRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/securities/holdings/{holdingId}/identifiers",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SecurityIdentifierResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteHoldingIdentifierAsync(Guid holdingId, Guid identifierId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/v1/securities/holdings/{holdingId}/identifiers/{identifierId}"
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HoldingResponse?> CorrectHoldingSecurityAsync(
        Guid holdingId,
        CorrectHoldingSecurityRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/securities/holdings/{holdingId}/correct",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<HoldingResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<SecurityCatalogItemResponse?> FetchPriceAsync(Guid securityId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/v1/securities/{securityId}/fetch-price",
                null
            );
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SecurityCatalogItemResponse>();
        }
        catch
        {
            return null;
        }
    }
}
