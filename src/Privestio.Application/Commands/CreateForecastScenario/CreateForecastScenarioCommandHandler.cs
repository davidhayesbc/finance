using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateForecastScenario;

public class CreateForecastScenarioCommandHandler
    : IRequestHandler<CreateForecastScenarioCommand, ForecastScenarioResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateForecastScenarioCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ForecastScenarioResponse> Handle(
        CreateForecastScenarioCommand request,
        CancellationToken cancellationToken
    )
    {
        var scenario = new ForecastScenario(request.UserId, request.Name, request.Description);

        var assumptions = request
            .GrowthAssumptions.Select(dto => new GrowthAssumption(
                dto.AccountId,
                dto.AccountType is not null ? Enum.Parse<AccountType>(dto.AccountType) : null,
                dto.AnnualGrowthRate,
                dto.AnnualInflationRate
            ))
            .ToList();

        scenario.UpdateAssumptions(assumptions);

        await _unitOfWork.ForecastScenarios.AddAsync(scenario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ForecastScenarioMapper.ToResponse(scenario);
    }
}
