using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.AcceptHouseholdInvitation;

public class AcceptHouseholdInvitationCommandHandler
    : IRequestHandler<AcceptHouseholdInvitationCommand, HouseholdResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public AcceptHouseholdInvitationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HouseholdResponse> Handle(
        AcceptHouseholdInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        var alreadyMember = await _unitOfWork.Households.IsUserInAnyHouseholdAsync(
            request.AcceptingUserId,
            cancellationToken
        );
        if (alreadyMember)
            throw new InvalidOperationException(
                "User is already a member of a household. Leave the current household before accepting a new invitation."
            );

        var invitation = await _unitOfWork.Households.GetInvitationByTokenAsync(
            request.Token,
            cancellationToken
        ) ?? throw new KeyNotFoundException("Invitation not found or has expired.");

        // Validates the claiming email and marks invitation accepted.
        invitation.Accept(request.AcceptingEmail);

        // Add the user to the household with the invited role.
        invitation.Household.AddMember(request.AcceptingUserId, invitation.Role);

        await _unitOfWork.Households.UpdateAsync(invitation.Household, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var loaded = await _unitOfWork.Households.GetByIdWithMembersAsync(
            invitation.HouseholdId,
            cancellationToken
        );

        return HouseholdMapper.ToResponse(loaded!);
    }
}
