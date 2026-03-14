namespace Privestio.Contracts.Requests;

public record CreateHoldingRequest
{
    public string Symbol { get; init; } = string.Empty;
    public string SecurityName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal AverageCostPerUnit { get; init; }
    public string Currency { get; init; } = "CAD";
    public string? Notes { get; init; }
}
