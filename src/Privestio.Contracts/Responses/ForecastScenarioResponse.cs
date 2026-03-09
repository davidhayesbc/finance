using Privestio.Contracts.Requests;

namespace Privestio.Contracts.Responses;

public record ForecastScenarioResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public IReadOnlyList<GrowthAssumptionDto> GrowthAssumptions { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
