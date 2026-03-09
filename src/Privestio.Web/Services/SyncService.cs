using System.Net.Http.Json;
using System.Text.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface ISyncService
{
    Task QueueMutationAsync(
        string entityType,
        string operationType,
        string entityId,
        string? payload
    );
    Task FlushQueueAsync();
    Task PullChangesAsync();
    Task SyncAsync();
}

public class SyncService : ISyncService
{
    private readonly IIndexedDbService _indexedDbService;
    private readonly HttpClient _httpClient;
    private readonly IConnectivityService _connectivityService;
    private string _deviceId = string.Empty;
    private string? _lastSyncToken;

    public SyncService(
        IIndexedDbService indexedDbService,
        HttpClient httpClient,
        IConnectivityService connectivityService
    )
    {
        _indexedDbService = indexedDbService;
        _httpClient = httpClient;
        _connectivityService = connectivityService;
        _deviceId = Guid.NewGuid().ToString("N")[..12];
    }

    public async Task QueueMutationAsync(
        string entityType,
        string operationType,
        string entityId,
        string? payload
    )
    {
        var operation = new
        {
            entityType,
            operationType,
            entityId,
            payload,
            timestamp = DateTime.UtcNow.ToString("O"),
        };

        await _indexedDbService.AddToSyncQueueAsync(operation);
    }

    public async Task FlushQueueAsync()
    {
        if (!_connectivityService.IsOnline)
            return;

        var queue = await _indexedDbService.GetSyncQueueAsync();
        if (queue.Count == 0)
            return;

        foreach (var entry in queue)
        {
            try
            {
                var endpoint = GetEndpointForOperation(entry);
                if (endpoint is null)
                    continue;

                HttpResponseMessage response;

                switch (entry.OperationType.ToLowerInvariant())
                {
                    case "create":
                        response = await _httpClient.PostAsJsonAsync(
                            endpoint,
                            JsonSerializer.Deserialize<object>(entry.Payload ?? "{}")
                        );
                        break;

                    case "update":
                        response = await _httpClient.PutAsJsonAsync(
                            endpoint,
                            JsonSerializer.Deserialize<object>(entry.Payload ?? "{}")
                        );
                        break;

                    case "delete":
                        response = await _httpClient.DeleteAsync(endpoint);
                        break;

                    default:
                        continue;
                }

                if (
                    !response.IsSuccessStatusCode
                    && response.StatusCode == System.Net.HttpStatusCode.Conflict
                )
                {
                    // Conflict detected -- store for resolution
                    var serverData = await response.Content.ReadAsStringAsync();
                    await _indexedDbService.PutItemAsync(
                        "syncConflicts",
                        new
                        {
                            id = entry.EntityId,
                            entityType = entry.EntityType,
                            localData = entry.Payload,
                            serverData,
                            detectedAt = DateTime.UtcNow.ToString("O"),
                        }
                    );
                }
            }
            catch
            {
                // If we fail to flush, stop processing; will retry next sync cycle
                return;
            }
        }

        await _indexedDbService.ClearSyncQueueAsync();
    }

    public async Task PullChangesAsync()
    {
        if (!_connectivityService.IsOnline)
            return;

        try
        {
            var url = $"/api/v1/sync/changes?deviceId={_deviceId}";
            if (!string.IsNullOrEmpty(_lastSyncToken))
                url += $"&sinceToken={Uri.EscapeDataString(_lastSyncToken)}";

            var response = await _httpClient.GetFromJsonAsync<SyncChangesResponse>(url);
            if (response is null)
                return;

            foreach (var change in response.Changes)
            {
                var storeName = change.EntityType.ToLowerInvariant() + "s";

                switch (change.ChangeType)
                {
                    case "Created":
                    case "Updated":
                        if (change.Payload is not null)
                        {
                            var entity = JsonSerializer.Deserialize<object>(change.Payload);
                            if (entity is not null)
                                await _indexedDbService.PutItemAsync(storeName, entity);
                        }
                        break;

                    case "Deleted":
                        await _indexedDbService.DeleteItemAsync(
                            storeName,
                            change.EntityId.ToString()
                        );
                        break;
                }
            }

            _lastSyncToken = response.SyncToken;

            // If there are more changes, pull again
            if (response.HasMore)
                await PullChangesAsync();
        }
        catch
        {
            // Silently fail -- will retry next sync cycle
        }
    }

    public async Task SyncAsync()
    {
        // Push local changes first, then pull server changes
        await FlushQueueAsync();
        await PullChangesAsync();
    }

    private static string? GetEndpointForOperation(SyncQueueEntry entry)
    {
        var baseRoute = entry.EntityType.ToLowerInvariant() switch
        {
            "account" => "/api/v1/accounts",
            "transaction" => "/api/v1/transactions",
            "category" => "/api/v1/categories",
            "budget" => "/api/v1/budgets",
            _ => null,
        };

        if (baseRoute is null)
            return null;

        return entry.OperationType.ToLowerInvariant() switch
        {
            "create" => baseRoute,
            "update" => $"{baseRoute}/{entry.EntityId}",
            "delete" => $"{baseRoute}/{entry.EntityId}",
            _ => null,
        };
    }
}
