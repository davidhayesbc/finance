namespace Privestio.Domain.Entities;

public class SyncTombstone : BaseEntity
{
    private SyncTombstone() { }

    public SyncTombstone(string entityType, Guid entityId, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        DeletedAtUtc = DateTime.UtcNow;
    }

    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime DeletedAtUtc { get; private set; }
    public DateTime? SyncedAt { get; private set; }

    public void MarkSynced()
    {
        SyncedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
