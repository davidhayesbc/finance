namespace Privestio.Contracts.Requests;

public record UpdateForecastScenarioRequest(
    string Name,
    string? Description,
    List<GrowthAssumptionDto> GrowthAssumptions
);
