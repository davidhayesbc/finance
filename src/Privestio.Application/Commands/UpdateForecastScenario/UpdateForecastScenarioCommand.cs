using MediatR;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.UpdateForecastScenario;

public record UpdateForecastScenarioCommand(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    List<GrowthAssumptionDto> GrowthAssumptions,
    bool IsDefault
) : IRequest<ForecastScenarioResponse>;
