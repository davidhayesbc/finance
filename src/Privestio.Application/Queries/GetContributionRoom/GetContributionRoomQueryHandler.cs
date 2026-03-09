using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetContributionRoom;

public class GetContributionRoomQueryHandler
    : IRequestHandler<GetContributionRoomQuery, IReadOnlyList<ContributionRoomResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetContributionRoomQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ContributionRoomResponse>> Handle(
        GetContributionRoomQuery request,
        CancellationToken cancellationToken
    )
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {request.AccountId} not found.");

        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException("Cannot view another user's contribution room.");

        var rooms = await _unitOfWork.ContributionRooms.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken
        );
        return rooms.Select(ContributionRoomMapper.ToResponse).ToList().AsReadOnly();
    }
}
