using Privestio.Domain.Enums;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents an in-app notification or alert for a user.
/// </summary>
public class Notification : BaseEntity
{
    private Notification() { }

    public Notification(
        Guid userId,
        string type,
        NotificationSeverity severity,
        string title,
        string message,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        UserId = userId;
        Type = type;
        Severity = severity;
        Title = title;
        Message = message;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public string Type { get; private set; } = string.Empty;
    public NotificationSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }

    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkAsUnread()
    {
        if (IsRead)
        {
            IsRead = false;
            ReadAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
