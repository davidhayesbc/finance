using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IAccountService
{
    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync();
    Task<AccountResponse?> GetAccountByIdAsync(Guid id);
    Task<AccountValueHistoryResponse?> GetAccountValueHistoryAsync(
        Guid id,
        DateOnly? fromDate = null,
        DateOnly? toDate = null
    );
    Task<AccountResponse?> CreateAccountAsync(CreateAccountRequest request);
}

public class AccountService : IAccountService
{
    private readonly HttpClient _httpClient;

    public AccountService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync()
    {
        try
        {
            var accounts = await _httpClient.GetFromJsonAsync<List<AccountResponse>>(
                "/api/v1/accounts"
            );
            return accounts ?? new List<AccountResponse>();
        }
        catch
        {
            return new List<AccountResponse>();
        }
    }

    public async Task<AccountResponse?> GetAccountByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AccountResponse>($"/api/v1/accounts/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<AccountValueHistoryResponse?> GetAccountValueHistoryAsync(
        Guid id,
        DateOnly? fromDate = null,
        DateOnly? toDate = null
    )
    {
        try
        {
            var parameters = new List<string>();
            if (fromDate.HasValue)
                parameters.Add($"fromDate={fromDate:yyyy-MM-dd}");
            if (toDate.HasValue)
                parameters.Add($"toDate={toDate:yyyy-MM-dd}");

            var query = parameters.Count == 0 ? string.Empty : $"?{string.Join("&", parameters)}";
            return await _httpClient.GetFromJsonAsync<AccountValueHistoryResponse>(
                $"/api/v1/accounts/{id}/history{query}"
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<AccountResponse?> CreateAccountAsync(CreateAccountRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/accounts", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<AccountResponse>();
        }
        catch
        {
            return null;
        }
    }
}
