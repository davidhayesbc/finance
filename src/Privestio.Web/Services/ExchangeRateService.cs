using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IExchangeRateService
{
    Task<IReadOnlyList<ExchangeRateResponse>> GetRatesAsync();
    Task<ExchangeRateResponse?> CreateAsync(CreateExchangeRateRequest request);
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;

    public ExchangeRateService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<ExchangeRateResponse>> GetRatesAsync()
    {
        try
        {
            var rates = await _httpClient.GetFromJsonAsync<List<ExchangeRateResponse>>(
                "/api/v1/exchange-rates"
            );
            return rates ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ExchangeRateResponse?> CreateAsync(CreateExchangeRateRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/exchange-rates", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
        }
        catch
        {
            return null;
        }
    }
}
