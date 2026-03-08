using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface ISinkingFundService
{
    Task<IReadOnlyList<SinkingFundResponse>> GetSinkingFundsAsync(bool activeOnly = false);
    Task<SinkingFundResponse?> CreateSinkingFundAsync(CreateSinkingFundRequest request);
    Task<SinkingFundResponse?> UpdateSinkingFundAsync(Guid id, UpdateSinkingFundRequest request);
    Task<SinkingFundResponse?> ContributeAsync(Guid id, ContributeSinkingFundRequest request);
    Task<bool> DeleteSinkingFundAsync(Guid id);
}

public class SinkingFundService : ISinkingFundService
{
    private readonly HttpClient _httpClient;

    public SinkingFundService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<SinkingFundResponse>> GetSinkingFundsAsync(
        bool activeOnly = false
    )
    {
        try
        {
            var url = "/api/v1/sinking-funds";
            if (activeOnly)
                url += "?activeOnly=true";

            var funds = await _httpClient.GetFromJsonAsync<List<SinkingFundResponse>>(url);
            return funds ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<SinkingFundResponse?> CreateSinkingFundAsync(CreateSinkingFundRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/sinking-funds", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<SinkingFundResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<SinkingFundResponse?> UpdateSinkingFundAsync(
        Guid id,
        UpdateSinkingFundRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/sinking-funds/{id}", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<SinkingFundResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<SinkingFundResponse?> ContributeAsync(
        Guid id,
        ContributeSinkingFundRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/sinking-funds/{id}/contribute",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<SinkingFundResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteSinkingFundAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/sinking-funds/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
