using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IConflictResolutionWebService
{
    Task<IReadOnlyList<SyncConflictResponse>> GetPendingConflictsAsync();
    Task<SyncConflictResponse?> ResolveConflictAsync(ResolveConflictRequest request);
}

public class ConflictResolutionWebService : IConflictResolutionWebService
{
    private readonly HttpClient _httpClient;

    public ConflictResolutionWebService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<SyncConflictResponse>> GetPendingConflictsAsync()
    {
        try
        {
            var conflicts = await _httpClient.GetFromJsonAsync<List<SyncConflictResponse>>(
                "/api/v1/sync/conflicts"
            );
            return conflicts ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<SyncConflictResponse?> ResolveConflictAsync(ResolveConflictRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/sync/conflicts/{request.ConflictId}/resolve",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<SyncConflictResponse>();
        }
        catch
        {
            return null;
        }
    }
}
