namespace Privestio.Domain.Entities;

public class SyncCheckpoint : BaseEntity
{
    private SyncCheckpoint() { }

    public SyncCheckpoint(Guid userId, string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        UserId = userId;
        DeviceId = deviceId;
        LastSyncToken = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public DateTime LastSyncToken { get; private set; }

    public void UpdateToken(DateTime token)
    {
        LastSyncToken = token;
        UpdatedAt = DateTime.UtcNow;
    }
}
