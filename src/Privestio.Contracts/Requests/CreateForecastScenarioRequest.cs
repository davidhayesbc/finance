namespace Privestio.Contracts.Requests;

public record CreateForecastScenarioRequest(
    string Name,
    string? Description,
    List<GrowthAssumptionDto> GrowthAssumptions
);

public record GrowthAssumptionDto(
    Guid? AccountId,
    string? AccountType,
    decimal AnnualGrowthRate,
    decimal AnnualInflationRate
);
