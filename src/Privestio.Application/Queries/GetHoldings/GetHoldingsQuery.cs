using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetHoldings;

public record GetHoldingsQuery(Guid AccountId, Guid UserId)
    : IRequest<IReadOnlyList<HoldingResponse>>;
