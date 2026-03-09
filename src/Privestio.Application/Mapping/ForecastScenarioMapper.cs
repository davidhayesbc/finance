using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class ForecastScenarioMapper
{
    public static ForecastScenarioResponse ToResponse(ForecastScenario scenario) =>
        new()
        {
            Id = scenario.Id,
            Name = scenario.Name,
            Description = scenario.Description,
            IsDefault = scenario.IsDefault,
            GrowthAssumptions = scenario
                .GrowthAssumptions.Select(g => new GrowthAssumptionDto(
                    g.AccountId,
                    g.AccountType?.ToString(),
                    g.AnnualGrowthRate,
                    g.AnnualInflationRate
                ))
                .ToList(),
            CreatedAt = scenario.CreatedAt,
            UpdatedAt = scenario.UpdatedAt,
        };
}
