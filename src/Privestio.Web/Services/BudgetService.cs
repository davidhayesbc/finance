using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetResponse>> GetBudgetsAsync(int? year = null, int? month = null);
    Task<IReadOnlyList<BudgetSummaryResponse>> GetBudgetSummaryAsync(int year, int month);
    Task<BudgetResponse?> CreateBudgetAsync(CreateBudgetRequest request);
    Task<BudgetResponse?> UpdateBudgetAsync(Guid id, UpdateBudgetRequest request);
    Task<bool> DeleteBudgetAsync(Guid id);
}

public class BudgetService : IBudgetService
{
    private readonly HttpClient _httpClient;

    public BudgetService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<BudgetResponse>> GetBudgetsAsync(
        int? year = null,
        int? month = null
    )
    {
        try
        {
            var url = "/api/v1/budgets";
            var queryParams = new List<string>();
            if (year.HasValue)
                queryParams.Add($"year={year.Value}");
            if (month.HasValue)
                queryParams.Add($"month={month.Value}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var budgets = await _httpClient.GetFromJsonAsync<List<BudgetResponse>>(url);
            return budgets ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<BudgetSummaryResponse>> GetBudgetSummaryAsync(
        int year,
        int month
    )
    {
        try
        {
            var summary = await _httpClient.GetFromJsonAsync<List<BudgetSummaryResponse>>(
                $"/api/v1/budgets/summary/{year}/{month}"
            );
            return summary ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<BudgetResponse?> CreateBudgetAsync(CreateBudgetRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/budgets", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<BudgetResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<BudgetResponse?> UpdateBudgetAsync(Guid id, UpdateBudgetRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/budgets/{id}", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<BudgetResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteBudgetAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/budgets/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
