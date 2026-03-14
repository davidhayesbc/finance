using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface ILotService
{
    Task<IReadOnlyList<LotResponse>> GetLotsAsync(Guid holdingId);
}

public class LotService : ILotService
{
    private readonly HttpClient _httpClient;

    public LotService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<LotResponse>> GetLotsAsync(Guid holdingId)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<LotResponse>>(
                $"/api/v1/holdings/{holdingId}/lots"
            );
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }
}
