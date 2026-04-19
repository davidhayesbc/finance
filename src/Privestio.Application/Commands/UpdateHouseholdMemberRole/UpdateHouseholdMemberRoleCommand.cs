using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateHouseholdMemberRole;

public record UpdateHouseholdMemberRoleCommand(
    Guid HouseholdId,
    Guid UserId,
    string NewRole,
    Guid RequestingUserId
) : IRequest<HouseholdMemberResponse>;
