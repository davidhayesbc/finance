namespace Privestio.Contracts.Responses;

public record TransactionSplitResponse
{
    public Guid Id { get; init; }
    public Guid TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? Notes { get; init; }
    public decimal? Percentage { get; init; }
}
