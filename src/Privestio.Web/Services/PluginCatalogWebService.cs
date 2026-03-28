using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IPluginCatalogWebService
{
    Task<PluginCatalogResponse?> GetPluginCatalogAsync();
}

public class PluginCatalogWebService : IPluginCatalogWebService
{
    private readonly HttpClient _httpClient;

    public PluginCatalogWebService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<PluginCatalogResponse?> GetPluginCatalogAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PluginCatalogResponse>("/api/v1/plugins");
        }
        catch
        {
            return null;
        }
    }
}
