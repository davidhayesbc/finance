namespace Privestio.Contracts.Requests;

public record CreateTransactionRequest
{
    public Guid AccountId { get; init; }
    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "CAD";
    public string Description { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public Guid? PayeeId { get; init; }
    public string? Notes { get; init; }
}
