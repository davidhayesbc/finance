using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetValuations;

public record GetValuationsQuery(Guid AccountId, Guid UserId)
    : IRequest<IReadOnlyList<ValuationResponse>>;
