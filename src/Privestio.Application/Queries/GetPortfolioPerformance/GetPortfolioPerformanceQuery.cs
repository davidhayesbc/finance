using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPortfolioPerformance;

public record GetPortfolioPerformanceQuery(Guid AccountId, Guid UserId)
    : IRequest<PortfolioPerformanceResponse?>;
