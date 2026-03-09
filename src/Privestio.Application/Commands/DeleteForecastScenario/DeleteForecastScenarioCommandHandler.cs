using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteForecastScenario;

public class DeleteForecastScenarioCommandHandler : IRequestHandler<DeleteForecastScenarioCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteForecastScenarioCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        DeleteForecastScenarioCommand request,
        CancellationToken cancellationToken
    )
    {
        var scenario =
            await _unitOfWork.ForecastScenarios.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"ForecastScenario {request.Id} not found.");

        if (scenario.UserId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot delete another user's forecast scenario."
            );

        await _unitOfWork.ForecastScenarios.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
