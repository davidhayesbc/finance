using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IForecastScenarioService
{
    Task<IReadOnlyList<ForecastScenarioResponse>> GetAllAsync();
    Task<ForecastScenarioResponse?> CreateAsync(CreateForecastScenarioRequest request);
    Task<ForecastScenarioResponse?> UpdateAsync(Guid id, UpdateForecastScenarioRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<NetWorthForecastResponse?> GetForecastAsync(Guid scenarioId, int months = 60);
}

public class ForecastScenarioService : IForecastScenarioService
{
    private readonly HttpClient _httpClient;

    public ForecastScenarioService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<ForecastScenarioResponse>> GetAllAsync()
    {
        try
        {
            var scenarios = await _httpClient.GetFromJsonAsync<List<ForecastScenarioResponse>>(
                "/api/v1/forecast-scenarios"
            );
            return scenarios ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ForecastScenarioResponse?> CreateAsync(CreateForecastScenarioRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/forecast-scenarios", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ForecastScenarioResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<ForecastScenarioResponse?> UpdateAsync(
        Guid id,
        UpdateForecastScenarioRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/forecast-scenarios/{id}",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ForecastScenarioResponse>();
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
            var response = await _httpClient.DeleteAsync($"/api/v1/forecast-scenarios/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<NetWorthForecastResponse?> GetForecastAsync(Guid scenarioId, int months = 60)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<NetWorthForecastResponse>(
                $"/api/v1/forecast-scenarios/{scenarioId}/forecast?months={months}"
            );
        }
        catch
        {
            return null;
        }
    }
}
