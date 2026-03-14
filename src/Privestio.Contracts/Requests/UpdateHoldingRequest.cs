namespace Privestio.Contracts.Requests;

public record UpdateHoldingRequest
{
    public string SecurityName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal AverageCostPerUnit { get; init; }
    public string Currency { get; init; } = "CAD";
    public string? Notes { get; init; }
}
