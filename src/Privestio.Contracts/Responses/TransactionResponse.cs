namespace Privestio.Contracts.Responses;

public record TransactionResponse
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public Guid? PayeeId { get; init; }
    public string? PayeeName { get; init; }
    public bool IsReconciled { get; init; }
    public bool IsSplit { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
