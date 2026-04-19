using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetHousehold;

public class GetHouseholdQueryHandler : IRequestHandler<GetHouseholdQuery, HouseholdResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ResourcePermissionService _permissions;

    public GetHouseholdQueryHandler(
        IUnitOfWork unitOfWork,
        ResourcePermissionService permissions
    )
    {
        _unitOfWork = unitOfWork;
        _permissions = permissions;
    }

    public async Task<HouseholdResponse?> Handle(
        GetHouseholdQuery request,
        CancellationToken cancellationToken
    )
    {
        var household = await _unitOfWork.Households.GetByIdWithMembersAsync(
            request.HouseholdId,
            cancellationToken
        );

        if (household is null)
            return null;

        await _permissions.EnsureHouseholdMemberAsync(
            household.Id,
            request.RequestingUserId,
            cancellationToken
        );

        return HouseholdMapper.ToResponse(household);
    }
}
