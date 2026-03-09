namespace Privestio.Contracts.Responses;

public record ReconciliationPeriodResponse
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public DateOnly StatementDate { get; init; }
    public decimal StatementBalanceAmount { get; init; }
    public string Currency { get; init; } = "CAD";
    public string Status { get; init; } = string.Empty;
    public DateTime? LockedAt { get; init; }
    public Guid? LockedByUserId { get; init; }
    public string? UnlockReason { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
