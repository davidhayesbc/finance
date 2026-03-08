using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IRecurringTransactionService
{
    Task<IReadOnlyList<RecurringTransactionResponse>> GetRecurringTransactionsAsync(
        bool activeOnly = false
    );
    Task<RecurringTransactionResponse?> CreateAsync(CreateRecurringTransactionRequest request);
    Task<RecurringTransactionResponse?> UpdateAsync(
        Guid id,
        UpdateRecurringTransactionRequest request
    );
    Task<bool> DeleteAsync(Guid id);
    Task<int> GenerateAsync(DateTime? upToDate = null);
}

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly HttpClient _httpClient;

    public RecurringTransactionService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<RecurringTransactionResponse>> GetRecurringTransactionsAsync(
        bool activeOnly = false
    )
    {
        try
        {
            var url = "/api/v1/recurring-transactions";
            if (activeOnly)
                url += "?activeOnly=true";

            var items = await _httpClient.GetFromJsonAsync<List<RecurringTransactionResponse>>(url);
            return items ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<RecurringTransactionResponse?> CreateAsync(
        CreateRecurringTransactionRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/recurring-transactions",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<RecurringTransactionResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<RecurringTransactionResponse?> UpdateAsync(
        Guid id,
        UpdateRecurringTransactionRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/recurring-transactions/{id}",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<RecurringTransactionResponse>();
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
            var response = await _httpClient.DeleteAsync($"/api/v1/recurring-transactions/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GenerateAsync(DateTime? upToDate = null)
    {
        try
        {
            var url = "/api/v1/recurring-transactions/generate";
            if (upToDate.HasValue)
                url += $"?upToDate={upToDate.Value:yyyy-MM-dd}";

            var response = await _httpClient.PostAsync(url, null);
            if (!response.IsSuccessStatusCode)
                return 0;

            var result = await response.Content.ReadFromJsonAsync<GenerateResult>();
            return result?.GeneratedCount ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private record GenerateResult(int GeneratedCount);
}
