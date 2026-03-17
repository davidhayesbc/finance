namespace Privestio.Contracts.Responses;

public record HoldingResponse
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public Guid SecurityId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string SecurityName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal AverageCostPerUnit { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
