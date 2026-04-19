using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHousehold;

public record GetHouseholdQuery(Guid HouseholdId, Guid RequestingUserId)
    : IRequest<HouseholdResponse?>;
