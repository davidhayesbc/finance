using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.InviteHouseholdMember;

public class InviteHouseholdMemberCommandHandler
    : IRequestHandler<InviteHouseholdMemberCommand, HouseholdInvitationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResourcePermissionService _permissions;

    public InviteHouseholdMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ResourcePermissionService permissions
    )
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
    }

    public async Task<HouseholdInvitationResponse> Handle(
        InviteHouseholdMemberCommand request,
        CancellationToken cancellationToken
    )
    {
        var household = await _unitOfWork.Households.GetByIdWithMembersAsync(
            request.HouseholdId,
            cancellationToken
        ) ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        await _permissions.EnsureHouseholdRoleAsync(
            household.Id,
            request.InvitedByUserId,
            HouseholdRole.Admin,
            cancellationToken
        );

        var role = Enum.Parse<HouseholdRole>(request.Role);
        var invitation = household.CreateInvitation(request.Email, role, request.InvitedByUserId);

        await _unitOfWork.Households.UpdateAsync(household, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HouseholdMapper.ToInvitationResponse(invitation);
    }
}
