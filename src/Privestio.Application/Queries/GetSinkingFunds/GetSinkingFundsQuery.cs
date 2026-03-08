using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetSinkingFunds;

public record GetSinkingFundsQuery(Guid UserId, bool ActiveOnly = false)
    : IRequest<IReadOnlyList<SinkingFundResponse>>;
