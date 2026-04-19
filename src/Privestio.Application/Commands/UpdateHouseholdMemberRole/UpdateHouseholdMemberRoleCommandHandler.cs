using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.UpdateHouseholdMemberRole;

public class UpdateHouseholdMemberRoleCommandHandler
    : IRequestHandler<UpdateHouseholdMemberRoleCommand, HouseholdMemberResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResourcePermissionService _permissions;

    public UpdateHouseholdMemberRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ResourcePermissionService permissions
    )
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
    }

    public async Task<HouseholdMemberResponse> Handle(
        UpdateHouseholdMemberRoleCommand request,
        CancellationToken cancellationToken
    )
    {
        var household = await _unitOfWork.Households.GetByIdWithMembersAsync(
            request.HouseholdId,
            cancellationToken
        ) ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        // Only Owner can change roles.
        await _permissions.EnsureHouseholdOwnerAsync(
            household.Id,
            request.RequestingUserId,
            cancellationToken
        );

        var newRole = Enum.Parse<HouseholdRole>(request.NewRole);
        household.UpdateMemberRole(request.UserId, newRole);

        await _unitOfWork.Households.UpdateAsync(household, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var member = household.Members.First(m => m.UserId == request.UserId);
        return HouseholdMapper.ToMemberResponse(member);
    }
}
