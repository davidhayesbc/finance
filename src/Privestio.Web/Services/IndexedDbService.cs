using Microsoft.JSInterop;

namespace Privestio.Web.Services;

public interface IIndexedDbService
{
    Task OpenDatabaseAsync();
    Task PutItemAsync<T>(string storeName, T item);
    Task<T?> GetItemAsync<T>(string storeName, string key);
    Task<List<T>> GetAllItemsAsync<T>(string storeName);
    Task DeleteItemAsync(string storeName, string key);
    Task ClearStoreAsync(string storeName);
    Task<int> AddToSyncQueueAsync(object operation);
    Task<List<SyncQueueEntry>> GetSyncQueueAsync();
    Task ClearSyncQueueAsync();
}

public class SyncQueueEntry
{
    public int OperationId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public class IndexedDbService : IIndexedDbService
{
    private readonly IJSRuntime _jsRuntime;

    public IndexedDbService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task OpenDatabaseAsync()
    {
        await _jsRuntime.InvokeVoidAsync("indexedDbFunctions.openDatabase");
    }

    public async Task PutItemAsync<T>(string storeName, T item)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDbFunctions.putItem", storeName, item);
    }

    public async Task<T?> GetItemAsync<T>(string storeName, string key)
    {
        return await _jsRuntime.InvokeAsync<T?>("indexedDbFunctions.getItem", storeName, key);
    }

    public async Task<List<T>> GetAllItemsAsync<T>(string storeName)
    {
        var result = await _jsRuntime.InvokeAsync<List<T>>(
            "indexedDbFunctions.getAllItems",
            storeName
        );
        return result ?? new List<T>();
    }

    public async Task DeleteItemAsync(string storeName, string key)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDbFunctions.deleteItem", storeName, key);
    }

    public async Task ClearStoreAsync(string storeName)
    {
        await _jsRuntime.InvokeVoidAsync("indexedDbFunctions.clearStore", storeName);
    }

    public async Task<int> AddToSyncQueueAsync(object operation)
    {
        return await _jsRuntime.InvokeAsync<int>("indexedDbFunctions.addToSyncQueue", operation);
    }

    public async Task<List<SyncQueueEntry>> GetSyncQueueAsync()
    {
        var result = await _jsRuntime.InvokeAsync<List<SyncQueueEntry>>(
            "indexedDbFunctions.getSyncQueue"
        );
        return result ?? new List<SyncQueueEntry>();
    }

    public async Task ClearSyncQueueAsync()
    {
        await _jsRuntime.InvokeVoidAsync("indexedDbFunctions.clearSyncQueue");
    }
}
