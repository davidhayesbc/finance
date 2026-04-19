using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.RenameHousehold;

public record RenameHouseholdCommand(
    Guid HouseholdId,
    string Name,
    Guid RequestingUserId
) : IRequest<HouseholdResponse>;
