namespace Privestio.Contracts.Responses;

public record LotResponse
{
    public Guid Id { get; init; }
    public Guid HoldingId { get; init; }
    public DateOnly AcquiredDate { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Source { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
