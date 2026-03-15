namespace Privestio.Contracts.Responses;

public record HoldingPerformanceResponse
{
    public Guid HoldingId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string SecurityName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal? CurrentPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? MarketValue { get; init; }
    public decimal BookValue { get; init; }
    public decimal? GainLoss { get; init; }
    public decimal? GainLossPercent { get; init; }
    public decimal? MoneyWeightedReturn { get; init; }
    public DateOnly? PriceAsOfDate { get; init; }
    public bool IsPriceStale { get; init; }
    public string PriceSource { get; init; } = string.Empty;
}
