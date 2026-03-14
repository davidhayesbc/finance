namespace Privestio.Contracts.Responses;

public record PortfolioPerformanceResponse
{
    public Guid AccountId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal TotalBookValue { get; init; }
    public decimal? TotalMarketValue { get; init; }
    public decimal? TotalGainLoss { get; init; }
    public decimal? TotalGainLossPercent { get; init; }
    public decimal? PortfolioMoneyWeightedReturn { get; init; }
    public DateTime CalculatedAt { get; init; }
    public IReadOnlyList<HoldingPerformanceResponse> Holdings { get; init; } = [];
}
