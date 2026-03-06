using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IRuleService
{
    Task<IReadOnlyList<CategorizationRuleResponse>> GetRulesAsync();
    Task<CategorizationRuleResponse?> CreateRuleAsync(CreateCategorizationRuleRequest request);
    Task<CategorizationRuleResponse?> UpdateRuleAsync(
        Guid id,
        UpdateCategorizationRuleRequest request
    );
    Task<bool> DeleteRuleAsync(Guid id);
}

public class RuleService : IRuleService
{
    private readonly HttpClient _httpClient;

    public RuleService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<CategorizationRuleResponse>> GetRulesAsync()
    {
        try
        {
            var rules = await _httpClient.GetFromJsonAsync<List<CategorizationRuleResponse>>(
                "/api/v1/rules"
            );
            return rules ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<CategorizationRuleResponse?> CreateRuleAsync(
        CreateCategorizationRuleRequest request
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/rules", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<CategorizationRuleResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<CategorizationRuleResponse?> UpdateRuleAsync(
        Guid id,
        UpdateCategorizationRuleRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/rules/{id}", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<CategorizationRuleResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteRuleAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/rules/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
