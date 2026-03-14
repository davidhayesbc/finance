using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IPortfolioService
{
    Task<PortfolioPerformanceResponse?> GetPerformanceAsync(Guid accountId);
}

public class PortfolioService : IPortfolioService
{
    private readonly HttpClient _http;

    public PortfolioService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PortfolioPerformanceResponse?> GetPerformanceAsync(Guid accountId)
    {
        try
        {
            return await _http.GetFromJsonAsync<PortfolioPerformanceResponse>(
                $"api/v1/accounts/{accountId}/performance"
            );
        }
        catch
        {
            return null;
        }
    }
}
