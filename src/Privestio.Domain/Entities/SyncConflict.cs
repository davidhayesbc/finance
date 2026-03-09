namespace Privestio.Domain.Entities;

public class SyncConflict : BaseEntity
{
    private SyncConflict() { }

    public SyncConflict(string entityType, Guid entityId, string localData, string serverData)
    {
        EntityType = entityType;
        EntityId = entityId;
        LocalData = localData;
        ServerData = serverData;
        Status = "Pending";
        DetectedAt = DateTime.UtcNow;
    }

    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string LocalData { get; private set; } = string.Empty;
    public string ServerData { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty; // Pending, Resolved
    public DateTime DetectedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? Resolution { get; private set; } // KeepLocal, KeepServer, Merged

    public void Resolve(string resolution, string? mergedData = null)
    {
        Status = "Resolved";
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;
        if (mergedData != null)
            LocalData = mergedData;
        UpdatedAt = DateTime.UtcNow;
    }
}
