using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.CreateHousehold;

public class CreateHouseholdCommandHandler : IRequestHandler<CreateHouseholdCommand, HouseholdResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateHouseholdCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HouseholdResponse> Handle(
        CreateHouseholdCommand request,
        CancellationToken cancellationToken
    )
    {
        var alreadyInHousehold = await _unitOfWork.Households.IsUserInAnyHouseholdAsync(
            request.OwnerId,
            cancellationToken
        );

        if (alreadyInHousehold)
            throw new InvalidOperationException(
                "User is already a member of a household. Leave the current household first."
            );

        var household = new Household(request.Name, request.OwnerId);

        // Sync the owner's HouseholdId on the User record.
        await _unitOfWork.Households.AddAsync(household, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-fetch with members loaded for the response.
        var loaded = await _unitOfWork.Households.GetByIdWithMembersAsync(
            household.Id,
            cancellationToken
        );

        return HouseholdMapper.ToResponse(loaded!);
    }
}
