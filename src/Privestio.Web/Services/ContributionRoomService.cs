using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IContributionRoomService
{
    Task<IReadOnlyList<ContributionRoomResponse>> GetByAccountIdAsync(Guid accountId);
    Task<ContributionRoomResponse?> UpdateAsync(Guid id, UpdateContributionRoomRequest request);
}

public class ContributionRoomService : IContributionRoomService
{
    private readonly HttpClient _httpClient;

    public ContributionRoomService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<ContributionRoomResponse>> GetByAccountIdAsync(Guid accountId)
    {
        try
        {
            var rooms = await _httpClient.GetFromJsonAsync<List<ContributionRoomResponse>>(
                $"/api/v1/contribution-room?accountId={accountId}"
            );
            return rooms ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ContributionRoomResponse?> UpdateAsync(
        Guid id,
        UpdateContributionRoomRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/contribution-room/{id}",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ContributionRoomResponse>();
        }
        catch
        {
            return null;
        }
    }
}
