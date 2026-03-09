using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetNetWorthForecast;

public record GetNetWorthForecastQuery(Guid UserId, Guid ScenarioId, int Months)
    : IRequest<NetWorthForecastResponse>;
