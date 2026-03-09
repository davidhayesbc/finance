using MediatR;

namespace Privestio.Application.Commands.DeleteForecastScenario;

public record DeleteForecastScenarioCommand(Guid Id, Guid UserId) : IRequest;
