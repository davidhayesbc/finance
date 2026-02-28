namespace Privestio.Domain.Entities;

/// <summary>
/// Append-only audit trail for all data mutations.
/// Records who changed what, when, and what the old/new values were.
/// </summary>
public class AuditEvent : BaseEntity
{
    private AuditEvent() { }

    public AuditEvent(
        Guid userId,
        string entityType,
        Guid entityId,
        string action,
        string? changedFields = null,
        string? oldValues = null,
        string? newValues = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        UserId = userId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        ChangedFields = changedFields;
        OldValues = oldValues;
        NewValues = newValues;
        Timestamp = DateTime.UtcNow;
    }

    public DateTime Timestamp { get; private set; }
    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;

    /// <summary>JSON array of changed field names.</summary>
    public string? ChangedFields { get; private set; }

    /// <summary>JSON object of old field values.</summary>
    public string? OldValues { get; private set; }

    /// <summary>JSON object of new field values.</summary>
    public string? NewValues { get; private set; }
}
