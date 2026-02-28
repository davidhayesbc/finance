using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IAccountService
{
    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync();
    Task<AccountResponse?> GetAccountByIdAsync(Guid id);
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
            var accounts = await _httpClient.GetFromJsonAsync<List<AccountResponse>>("/api/v1/accounts");
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

    public async Task<AccountResponse?> CreateAccountAsync(CreateAccountRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/accounts", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AccountResponse>();
        }
        catch
        {
            return null;
        }
    }
}
