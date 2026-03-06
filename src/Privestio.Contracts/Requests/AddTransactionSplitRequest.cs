namespace Privestio.Contracts.Requests;

public record AddTransactionSplitRequest
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "CAD";
    public Guid CategoryId { get; init; }
    public string? Notes { get; init; }
    public decimal? Percentage { get; init; }
}
