using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IAmortizationService
{
    Task<AmortizationScheduleResponse?> GetScheduleAsync(Guid accountId);
    Task<AmortizationScheduleResponse?> GenerateAsync(GenerateAmortizationRequest request);
}

public class AmortizationService : IAmortizationService
{
    private readonly HttpClient _httpClient;

    public AmortizationService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<AmortizationScheduleResponse?> GetScheduleAsync(Guid accountId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AmortizationScheduleResponse>(
                $"/api/v1/amortization?accountId={accountId}"
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<AmortizationScheduleResponse?> GenerateAsync(
        GenerateAmortizationRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/amortization", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<AmortizationScheduleResponse>();
        }
        catch
        {
            return null;
        }
    }
}
