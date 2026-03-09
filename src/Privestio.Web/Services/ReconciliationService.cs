using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IReconciliationService
{
    Task<IReadOnlyList<ReconciliationPeriodResponse>> GetByAccountIdAsync(Guid accountId);
    Task<ReconciliationPeriodResponse?> CreateAsync(CreateReconciliationPeriodRequest request);
    Task<bool> LockAsync(Guid id);
    Task<bool> UnlockAsync(Guid id, UnlockReconciliationPeriodRequest request);
}

public class ReconciliationService : IReconciliationService
{
    private readonly HttpClient _httpClient;

    public ReconciliationService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<ReconciliationPeriodResponse>> GetByAccountIdAsync(
        Guid accountId
    )
    {
        try
        {
            var periods = await _httpClient.GetFromJsonAsync<List<ReconciliationPeriodResponse>>(
                $"/api/v1/reconciliation?accountId={accountId}"
            );
            return periods ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ReconciliationPeriodResponse?> CreateAsync(
        CreateReconciliationPeriodRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/reconciliation", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ReconciliationPeriodResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> LockAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/reconciliation/{id}/lock",
                new LockReconciliationPeriodRequest()
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnlockAsync(Guid id, UnlockReconciliationPeriodRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/reconciliation/{id}/unlock",
                request
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
