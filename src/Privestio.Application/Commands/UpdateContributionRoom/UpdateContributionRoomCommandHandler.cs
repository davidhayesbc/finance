using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateContributionRoom;

public class UpdateContributionRoomCommandHandler
    : IRequestHandler<UpdateContributionRoomCommand, ContributionRoomResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateContributionRoomCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ContributionRoomResponse> Handle(
        UpdateContributionRoomCommand request,
        CancellationToken cancellationToken
    )
    {
        var account =
            await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {request.AccountId} not found.");

        if (account.OwnerId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot update contribution room for another user's account."
            );

        var room = await _unitOfWork.ContributionRooms.GetByAccountIdAndYearAsync(
            request.AccountId,
            request.Year,
            cancellationToken
        );

        if (room is null)
        {
            room = new ContributionRoom(
                request.AccountId,
                request.Year,
                new Money(request.AnnualLimitAmount ?? 0m, request.Currency),
                new Money(request.CarryForwardAmount ?? 0m, request.Currency)
            );

            if (request.ContributionAmount.HasValue)
                room.RecordContribution(
                    new Money(request.ContributionAmount.Value, request.Currency)
                );

            await _unitOfWork.ContributionRooms.AddAsync(room, cancellationToken);
        }
        else
        {
            if (request.AnnualLimitAmount.HasValue)
                room.UpdateAnnualLimit(
                    new Money(request.AnnualLimitAmount.Value, request.Currency)
                );

            if (request.CarryForwardAmount.HasValue)
                room.SetCarryForward(new Money(request.CarryForwardAmount.Value, request.Currency));

            if (request.ContributionAmount.HasValue)
                room.RecordContribution(
                    new Money(request.ContributionAmount.Value, request.Currency)
                );

            await _unitOfWork.ContributionRooms.UpdateAsync(room, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ContributionRoomMapper.ToResponse(room);
    }
}
