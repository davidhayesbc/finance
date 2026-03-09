namespace Privestio.Contracts.Responses;

public record SyncConflictResponse
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string LocalData { get; init; } = string.Empty;
    public string ServerData { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? Resolution { get; init; }
}
