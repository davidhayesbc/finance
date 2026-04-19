using MediatR;

namespace Privestio.Application.Commands.RemoveHouseholdMember;

public record RemoveHouseholdMemberCommand(
    Guid HouseholdId,
    Guid UserIdToRemove,
    Guid RequestingUserId
) : IRequest;
