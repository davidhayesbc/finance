using System.Net.Http.Json;
using Privestio.Contracts.Pagination;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface ITransactionService
{
    Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(
        Guid accountId,
        int pageSize = 20,
        string? cursor = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? categoryId = null
    );
    Task<TransactionResponse?> GetTransactionByIdAsync(Guid id);
    Task<IReadOnlyList<TransactionSplitResponse>?> UpdateSplitsAsync(
        Guid transactionId,
        UpdateTransactionSplitsRequest request
    );
}

public class TransactionService : ITransactionService
{
    private readonly HttpClient _httpClient;

    public TransactionService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(
        Guid accountId,
        int pageSize = 20,
        string? cursor = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? categoryId = null
    )
    {
        try
        {
            var url = $"/api/v1/transactions?accountId={accountId}&pageSize={pageSize}";
            if (cursor is not null)
                url += $"&cursor={Uri.EscapeDataString(cursor)}";
            if (fromDate.HasValue)
                url += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            if (toDate.HasValue)
                url += $"&toDate={toDate.Value:yyyy-MM-dd}";
            if (categoryId.HasValue)
                url += $"&categoryId={categoryId.Value}";

            var result = await _httpClient.GetFromJsonAsync<PagedResponse<TransactionResponse>>(
                url
            );
            return result ?? new PagedResponse<TransactionResponse>();
        }
        catch
        {
            return new PagedResponse<TransactionResponse>();
        }
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TransactionResponse>(
                $"/api/v1/transactions/{id}"
            );
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TransactionSplitResponse>?> UpdateSplitsAsync(
        Guid transactionId,
        UpdateTransactionSplitsRequest request
    )
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/v1/transactions/{transactionId}/splits",
                request
            );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<List<TransactionSplitResponse>>();
        }
        catch
        {
            return null;
        }
    }
}
