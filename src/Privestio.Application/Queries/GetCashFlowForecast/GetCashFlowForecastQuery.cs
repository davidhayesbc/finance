using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetCashFlowForecast;

public record GetCashFlowForecastQuery(Guid UserId, int Months = 6)
    : IRequest<CashFlowForecastResponse>;
