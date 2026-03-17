using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.DataLoader;

public class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public ApiClient(string baseUrl, ILogger<ApiClient> logger)
    {
        _logger = logger;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/')) };
    }

    public void SetToken(string token) =>
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest { Email = email, Password = password },
            JsonOptions
        );
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
    }

    public async Task<AuthResponse?> RegisterAsync(
        string email,
        string password,
        string displayName
    )
    {
        var response = await _http.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
            },
            JsonOptions
        );
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Registration failed ({Status}): {Body}", response.StatusCode, body);
            return null;
        }
        return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
    }

    public async Task<bool> ClearLoaderDataAsync()
    {
        var response = await _http.PostAsync("/api/v1/ops/clear-loader-data", content: null);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Clear loader data failed ({Status}): {Body}",
                response.StatusCode,
                body
            );
            return false;
        }

        return true;
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<AccountResponse>>(
            "/api/v1/accounts",
            JsonOptions
        ) ?? [];

    public async Task<AccountResponse?> CreateAccountAsync(CreateAccountRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/accounts", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Create account failed ({Status}): {Body}", response.StatusCode, body);
            return null;
        }
        return await response.Content.ReadFromJsonAsync<AccountResponse>(JsonOptions);
    }

    public async Task<AccountResponse?> UpdateAccountAsync(
        Guid accountId,
        UpdateAccountRequest request
    )
    {
        var response = await _http.PutAsJsonAsync(
            $"/api/v1/accounts/{accountId}",
            request,
            JsonOptions
        );
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Update account failed ({Status}): {Body}", response.StatusCode, body);
            return null;
        }
        return await response.Content.ReadFromJsonAsync<AccountResponse>(JsonOptions);
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<CategoryResponse>>(
            "/api/v1/categories",
            JsonOptions
        ) ?? [];

    public async Task<CategoryResponse?> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/categories", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Create category failed ({Status}): {Body}",
                response.StatusCode,
                body
            );
            return null;
        }
        return await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
    }

    public async Task<IReadOnlyList<PayeeResponse>> GetPayeesAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<PayeeResponse>>("/api/v1/payees", JsonOptions)
        ?? [];

    public async Task<PayeeResponse?> CreatePayeeAsync(CreatePayeeRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/payees", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Create payee failed ({Status}): {Body}", response.StatusCode, body);
            return null;
        }
        return await response.Content.ReadFromJsonAsync<PayeeResponse>(JsonOptions);
    }

    public async Task<IReadOnlyList<TagResponse>> GetTagsAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<TagResponse>>("/api/v1/tags", JsonOptions) ?? [];

    public async Task<TagResponse?> CreateTagAsync(CreateTagRequest request)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/tags", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Create tag failed ({Status}): {Body}", response.StatusCode, body);
            return null;
        }
        return await response.Content.ReadFromJsonAsync<TagResponse>(JsonOptions);
    }

    public async Task<IReadOnlyList<ImportMappingResponse>> GetImportMappingsAsync() =>
        await _http.GetFromJsonAsync<IReadOnlyList<ImportMappingResponse>>(
            "/api/v1/import-mappings",
            JsonOptions
        ) ?? [];

    public async Task<ImportMappingResponse?> CreateImportMappingAsync(
        CreateImportMappingRequest request
    )
    {
        var response = await _http.PostAsJsonAsync("/api/v1/import-mappings", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Create import mapping failed ({Status}): {Body}",
                response.StatusCode,
                body
            );
            return null;
        }
        return await response.Content.ReadFromJsonAsync<ImportMappingResponse>(JsonOptions);
    }

    public async Task<ImportResultResponse?> ImportFileAsync(
        Guid accountId,
        string filePath,
        Guid? mappingId = null,
        string? policy = null
    )
    {
        using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var url = $"/api/v1/import/{accountId}";
        var queryParams = new List<string>();
        if (mappingId.HasValue)
            queryParams.Add($"mappingId={mappingId.Value}");
        if (!string.IsNullOrEmpty(policy))
            queryParams.Add($"policy={policy}");
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        var response = await _http.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Import file failed ({Status}): {Body}", response.StatusCode, body);
            return null;
        }
        return await response.Content.ReadFromJsonAsync<ImportResultResponse>(JsonOptions);
    }

    public async Task<IReadOnlyList<ValuationResponse>> GetValuationsAsync(Guid accountId) =>
        await _http.GetFromJsonAsync<IReadOnlyList<ValuationResponse>>(
            $"/api/v1/accounts/{accountId}/valuations",
            JsonOptions
        ) ?? [];

    public async Task<ValuationResponse?> CreateValuationAsync(
        Guid accountId,
        CreateValuationRequest request
    )
    {
        var response = await _http.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/valuations",
            request,
            JsonOptions
        );
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Create valuation failed ({Status}): {Body}",
                response.StatusCode,
                body
            );
            return null;
        }
        return await response.Content.ReadFromJsonAsync<ValuationResponse>(JsonOptions);
    }

    public async Task<IReadOnlyList<PriceHistoryResponse>?> BatchCreatePriceHistoryAsync(
        BatchCreatePriceHistoryRequest request
    )
    {
        var response = await _http.PostAsJsonAsync(
            "/api/v1/price-history/batch",
            request,
            JsonOptions
        );
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "Batch create price history failed ({Status}): {Body}",
                response.StatusCode,
                body
            );
            return null;
        }
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PriceHistoryResponse>>(
            JsonOptions
        );
    }

    public void Dispose() => _http.Dispose();
}
