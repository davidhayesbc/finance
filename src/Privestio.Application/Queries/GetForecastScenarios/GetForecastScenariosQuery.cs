using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetForecastScenarios;

public record GetForecastScenariosQuery(Guid UserId)
    : IRequest<IReadOnlyList<ForecastScenarioResponse>>;
