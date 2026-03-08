namespace Privestio.Contracts.Responses;

public record RecurringTransactionResponse
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "CAD";
    public string TransactionType { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime NextOccurrence { get; init; }
    public DateTime? LastGenerated { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public Guid? PayeeId { get; init; }
    public string? PayeeName { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
