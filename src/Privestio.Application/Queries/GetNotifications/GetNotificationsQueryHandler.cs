using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetNotifications;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNotificationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<NotificationResponse>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var notifications = await _unitOfWork.Notifications.GetByUserIdAsync(
            request.UserId,
            request.IncludeRead,
            request.Limit,
            cancellationToken
        );

        return notifications.Select(NotificationMapper.ToResponse).ToList().AsReadOnly();
    }
}
