using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateHousehold;

public record CreateHouseholdCommand(string Name, Guid OwnerId) : IRequest<HouseholdResponse>;
