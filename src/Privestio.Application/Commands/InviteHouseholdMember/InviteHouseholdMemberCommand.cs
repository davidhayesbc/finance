using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.InviteHouseholdMember;

public record InviteHouseholdMemberCommand(
    Guid HouseholdId,
    string Email,
    string Role,
    Guid InvitedByUserId
) : IRequest<HouseholdInvitationResponse>;
