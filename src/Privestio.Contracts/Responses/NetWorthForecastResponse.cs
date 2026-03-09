namespace Privestio.Contracts.Responses;

public record NetWorthForecastResponse
{
    public IReadOnlyList<NetWorthForecastPeriod> Periods { get; init; } = [];
    public string ScenarioName { get; init; } = string.Empty;
    public string Currency { get; init; } = "CAD";
}

public record NetWorthForecastPeriod
{
    public DateOnly Date { get; init; }
    public decimal ProjectedNetWorth { get; init; }
    public decimal ProjectedAssets { get; init; }
    public decimal ProjectedLiabilities { get; init; }
}
