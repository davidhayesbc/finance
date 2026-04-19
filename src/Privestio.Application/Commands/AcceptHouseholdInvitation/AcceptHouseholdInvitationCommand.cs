using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.AcceptHouseholdInvitation;

public record AcceptHouseholdInvitationCommand(
    Guid Token,
    Guid AcceptingUserId,
    string AcceptingEmail
) : IRequest<HouseholdResponse>;
