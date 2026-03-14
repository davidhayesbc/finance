namespace Privestio.Contracts.Requests;

public record CreateLotRequest
{
    public DateOnly AcquiredDate { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string Currency { get; init; } = "CAD";
    public string? Source { get; init; }
    public string? Notes { get; init; }
}
