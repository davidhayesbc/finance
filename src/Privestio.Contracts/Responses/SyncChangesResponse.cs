namespace Privestio.Contracts.Responses;

public record SyncChangesResponse
{
    public IReadOnlyList<SyncEntityChange> Changes { get; init; } = [];
    public string SyncToken { get; init; } = string.Empty;
    public bool HasMore { get; init; }
}

public record SyncEntityChange
{
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string ChangeType { get; init; } = string.Empty; // Created, Updated, Deleted
    public DateTime ChangedAt { get; init; }
    public string? Payload { get; init; } // JSON serialized entity
}
