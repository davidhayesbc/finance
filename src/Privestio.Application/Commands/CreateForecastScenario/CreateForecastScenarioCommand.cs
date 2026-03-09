using MediatR;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateForecastScenario;

public record CreateForecastScenarioCommand(
    Guid UserId,
    string Name,
    string? Description,
    List<GrowthAssumptionDto> GrowthAssumptions
) : IRequest<ForecastScenarioResponse>;
