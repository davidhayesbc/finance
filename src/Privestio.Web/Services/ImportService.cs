using System.Net.Http.Json;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface IImportService
{
    Task<ImportResultResponse?> ImportFileAsync(
        Guid accountId,
        Stream fileStream,
        string fileName,
        Guid? mappingId = null,
        string? policy = null
    );
    Task<IReadOnlyList<ImportBatchResponse>> GetImportBatchesAsync();
    Task<ImportBatchResponse?> GetImportBatchAsync(Guid batchId);
    Task<bool> RollbackImportAsync(Guid batchId);
    Task<FilePreviewResponse?> PreviewFileAsync(Stream fileStream, string fileName);
    Task<IReadOnlyList<ImportMappingResponse>> GetMappingsAsync();
    Task<ImportMappingResponse?> CreateMappingAsync(CreateImportMappingRequest request);
    Task<ImportMappingResponse?> UpdateMappingAsync(Guid id, UpdateImportMappingRequest request);
    Task<bool> DeleteMappingAsync(Guid id);
}

public class ImportService : IImportService
{
    private readonly HttpClient _httpClient;

    public ImportService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<ImportResultResponse?> ImportFileAsync(
        Guid accountId,
        Stream fileStream,
        string fileName,
        Guid? mappingId = null,
        string? policy = null
    )
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", fileName);

            var url = $"/api/v1/import/{accountId}";
            var queryParams = new List<string>();
            if (mappingId.HasValue)
                queryParams.Add($"mappingId={mappingId.Value}");
            if (policy is not null)
                queryParams.Add($"policy={policy}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ImportResultResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ImportBatchResponse>> GetImportBatchesAsync()
    {
        try
        {
            var batches = await _httpClient.GetFromJsonAsync<List<ImportBatchResponse>>(
                "/api/v1/import/batches"
            );
            return batches ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ImportBatchResponse?> GetImportBatchAsync(Guid batchId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ImportBatchResponse>(
                $"/api/v1/import/batches/{batchId}"
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RollbackImportAsync(Guid batchId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/v1/import/{batchId}/rollback", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FilePreviewResponse?> PreviewFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("/api/v1/import-mappings/preview", content);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<FilePreviewResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ImportMappingResponse>> GetMappingsAsync()
    {
        try
        {
            var mappings = await _httpClient.GetFromJsonAsync<List<ImportMappingResponse>>(
                "/api/v1/import-mappings"
            );
            return mappings ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ImportMappingResponse?> CreateMappingAsync(CreateImportMappingRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/import-mappings", request);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ImportMappingResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<ImportMappingResponse?> UpdateMappingAsync(
        Guid id,
        UpdateImportMappingRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/import-mappings/{id}",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ImportMappingResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteMappingAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/import-mappings/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
