using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IAnalyticsService
{
    Task<NetWorthSummaryResponse?> GetNetWorthSummaryAsync();
    Task<SpendingAnalysisResponse?> GetSpendingAnalysisAsync(DateOnly startDate, DateOnly endDate);
    Task<CashFlowSummaryResponse?> GetCashFlowSummaryAsync(DateOnly startDate, DateOnly endDate);
    Task<DebtOverviewResponse?> GetDebtOverviewAsync();
}

public class AnalyticsService : IAnalyticsService
{
    private readonly HttpClient _httpClient;

    public AnalyticsService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<NetWorthSummaryResponse?> GetNetWorthSummaryAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<NetWorthSummaryResponse>(
                "/api/v1/analytics/net-worth"
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<SpendingAnalysisResponse?> GetSpendingAnalysisAsync(
        DateOnly startDate,
        DateOnly endDate
    )
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SpendingAnalysisResponse>(
                $"/api/v1/analytics/spending?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<CashFlowSummaryResponse?> GetCashFlowSummaryAsync(
        DateOnly startDate,
        DateOnly endDate
    )
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CashFlowSummaryResponse>(
                $"/api/v1/analytics/cash-flow?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<DebtOverviewResponse?> GetDebtOverviewAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DebtOverviewResponse>(
                "/api/v1/analytics/debt"
            );
        }
        catch
        {
            return null;
        }
    }
}
