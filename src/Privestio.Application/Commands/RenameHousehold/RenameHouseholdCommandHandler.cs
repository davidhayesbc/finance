using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Commands.RenameHousehold;

public class RenameHouseholdCommandHandler : IRequestHandler<RenameHouseholdCommand, HouseholdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResourcePermissionService _permissions;

    public RenameHouseholdCommandHandler(
        IUnitOfWork unitOfWork,
        ResourcePermissionService permissions
    )
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
    }

    public async Task<HouseholdResponse> Handle(
        RenameHouseholdCommand request,
        CancellationToken cancellationToken
    )
    {
        var household = await _unitOfWork.Households.GetByIdWithMembersAsync(
            request.HouseholdId,
            cancellationToken
        ) ?? throw new KeyNotFoundException($"Household {request.HouseholdId} not found.");

        await _permissions.EnsureHouseholdRoleAsync(
            household.Id,
            request.RequestingUserId,
            HouseholdRole.Admin,
            cancellationToken
        );

        household.Rename(request.Name);

        await _unitOfWork.Households.UpdateAsync(household, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return HouseholdMapper.ToResponse(household);
    }
}
