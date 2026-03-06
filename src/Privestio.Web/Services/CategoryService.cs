using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync();
}

public class CategoryService : ICategoryService
{
    private readonly HttpClient _httpClient;

    public CategoryService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync()
    {
        try
        {
            var categories = await _httpClient.GetFromJsonAsync<List<CategoryResponse>>(
                "/api/v1/categories"
            );
            return categories ?? [];
        }
        catch
        {
            return [];
        }
    }
}
