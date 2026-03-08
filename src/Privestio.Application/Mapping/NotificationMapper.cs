using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class NotificationMapper
{
    public static NotificationResponse ToResponse(Notification notification) =>
        new()
        {
            Id = notification.Id,
            Type = notification.Type,
            Severity = notification.Severity.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            RelatedEntityType = notification.RelatedEntityType,
            RelatedEntityId = notification.RelatedEntityId,
            IsRead = notification.IsRead,
            CreatedAtUtc = notification.CreatedAtUtc,
            ReadAt = notification.ReadAt,
        };
}
