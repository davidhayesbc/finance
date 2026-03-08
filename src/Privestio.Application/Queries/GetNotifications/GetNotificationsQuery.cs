using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetNotifications;

public record GetNotificationsQuery(Guid UserId, bool IncludeRead = false, int Limit = 50)
    : IRequest<IReadOnlyList<NotificationResponse>>;
