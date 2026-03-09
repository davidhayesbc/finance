namespace Privestio.Contracts.Requests;

public record CreatePriceHistoryRequest
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "CAD";
    public DateOnly AsOfDate { get; init; }
    public string Source { get; init; } = string.Empty;
}
