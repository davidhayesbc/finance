using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface ISecurityWebService
{
    Task<IReadOnlyList<SecurityConflictResponse>> GetConflictsAsync();
    Task<IReadOnlyList<SecurityIdentifierResponse>> GetHoldingIdentifiersAsync(Guid holdingId);
    Task<SecurityIdentifierResponse?> AddHoldingIdentifierAsync(
        Guid holdingId,
        AddSecurityIdentifierRequest request
    );
    Task<bool> DeleteHoldingIdentifierAsync(Guid holdingId, Guid identifierId);
    Task<HoldingResponse?> CorrectHoldingSecurityAsync(Guid holdingId, CorrectHoldingSecurityRequest request);
}

public class SecurityWebService : ISecurityWebService
{
    private readonly HttpClient _httpClient;

    public SecurityWebService(HttpClient httpClient) => _httpClient = httpClient;

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

    public async Task<IReadOnlyList<SecurityIdentifierResponse>> GetHoldingIdentifiersAsync(Guid holdingId)
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
}
