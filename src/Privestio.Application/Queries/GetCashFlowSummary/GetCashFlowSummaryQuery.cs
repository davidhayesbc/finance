using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetCashFlowSummary;

public record GetCashFlowSummaryQuery(Guid UserId, DateOnly StartDate, DateOnly EndDate)
    : IRequest<CashFlowSummaryResponse>;
