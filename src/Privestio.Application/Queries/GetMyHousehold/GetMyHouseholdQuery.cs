using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetMyHousehold;

/// <summary>Returns the household the requesting user belongs to, if any.</summary>
public record GetMyHouseholdQuery(Guid UserId) : IRequest<HouseholdResponse?>;
