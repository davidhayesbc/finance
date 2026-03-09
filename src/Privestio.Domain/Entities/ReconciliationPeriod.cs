using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a reconciliation period for an account statement.
/// Supports open/balanced/locked workflow with audit trail for unlocks.
/// </summary>
public class ReconciliationPeriod : BaseEntity
{
    private ReconciliationPeriod() { }

    public ReconciliationPeriod(
        Guid accountId,
        DateOnly statementDate,
        Money statementBalance,
        string? notes = null
    )
    {
        AccountId = accountId;
        StatementDate = statementDate;
        StatementBalance = statementBalance;
        Status = ReconciliationStatus.Open;
        Notes = notes;
    }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public DateOnly StatementDate { get; private set; }
    public Money StatementBalance { get; private set; }
    public ReconciliationStatus Status { get; private set; }

    public DateTime? LockedAt { get; private set; }
    public Guid? LockedByUserId { get; private set; }
    public string? UnlockReason { get; private set; }
    public string? Notes { get; private set; }

    public void MarkBalanced()
    {
        Status = ReconciliationStatus.Balanced;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Lock(Guid userId)
    {
        if (Status != ReconciliationStatus.Balanced)
            throw new InvalidOperationException(
                "Only a balanced reconciliation period can be locked."
            );

        Status = ReconciliationStatus.Locked;
        LockedAt = DateTime.UtcNow;
        LockedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unlock(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != ReconciliationStatus.Locked)
            throw new InvalidOperationException(
                "Only a locked reconciliation period can be unlocked."
            );

        Status = ReconciliationStatus.Open;
        UnlockReason = reason;
        LockedAt = null;
        LockedByUserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
