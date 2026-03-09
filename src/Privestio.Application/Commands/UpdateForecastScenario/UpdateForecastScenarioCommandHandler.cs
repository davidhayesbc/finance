using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.UpdateForecastScenario;

public class UpdateForecastScenarioCommandHandler
    : IRequestHandler<UpdateForecastScenarioCommand, ForecastScenarioResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateForecastScenarioCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ForecastScenarioResponse> Handle(
        UpdateForecastScenarioCommand request,
        CancellationToken cancellationToken
    )
    {
        var scenario =
            await _unitOfWork.ForecastScenarios.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"ForecastScenario {request.Id} not found.");

        if (scenario.UserId != request.UserId)
            throw new UnauthorizedAccessException(
                "Cannot update another user's forecast scenario."
            );

        scenario.Rename(request.Name);
        scenario.UpdateDescription(request.Description);
        scenario.SetDefault(request.IsDefault);

        var assumptions = request
            .GrowthAssumptions.Select(dto => new GrowthAssumption(
                dto.AccountId,
                dto.AccountType is not null ? Enum.Parse<AccountType>(dto.AccountType) : null,
                dto.AnnualGrowthRate,
                dto.AnnualInflationRate
            ))
            .ToList();

        scenario.UpdateAssumptions(assumptions);

        await _unitOfWork.ForecastScenarios.UpdateAsync(scenario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ForecastScenarioMapper.ToResponse(scenario);
    }
}
