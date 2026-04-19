using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.RemoveHouseholdMember;

public class RemoveHouseholdMemberCommandHandler : IRequestHandler<RemoveHouseholdMemberCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResourcePermissionService _permissions;

    public RemoveHouseholdMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ResourcePermissionService permissions
    )
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
    }

    public async Task Handle(
        RemoveHouseholdMemberCommand request,
        CancellationToken cancellationToken
    )
    {
        var household = await _unitOfWork.Households.GetByIdWithMembersAsync(
            request.HouseholdId,
            cancellationToken
        ) ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        // Only Owner or Admin can remove members; members can remove themselves.
        if (request.RequestingUserId != request.UserIdToRemove)
        {
            await _permissions.EnsureHouseholdRoleAsync(
                household.Id,
                request.RequestingUserId,
                HouseholdRole.Admin,
                cancellationToken
            );
        }

        household.RemoveMember(request.UserIdToRemove);

        var removedUser = await _unitOfWork.Users.GetByIdAsync(
            request.UserIdToRemove,
            cancellationToken
        );
        if (removedUser is not null)
        {
            removedUser.HouseholdId = null;
            await _unitOfWork.Users.UpdateAsync(removedUser, cancellationToken);
        }

        await _unitOfWork.Households.UpdateAsync(household, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
