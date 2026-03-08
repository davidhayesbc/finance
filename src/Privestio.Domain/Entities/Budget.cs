using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a monthly budget allocation for a specific category.
/// Budgets are split-aware: actual spending is tracked via split line categories.
/// </summary>
public class Budget : BaseEntity
{
    private Budget() { }

    public Budget(
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        Money amount,
        bool rolloverEnabled = false,
        string? notes = null
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1900);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 2200);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        if (amount.Amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Budget amount must be non-negative."
            );

        UserId = userId;
        CategoryId = categoryId;
        Year = year;
        Month = month;
        Amount = amount;
        RolloverEnabled = rolloverEnabled;
        Notes = notes;
    }

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public Guid CategoryId { get; private set; }
    public Category? Category { get; set; }

    public int Year { get; private set; }
    public int Month { get; private set; }

    public Money Amount { get; private set; }
    public bool RolloverEnabled { get; private set; }
    public string? Notes { get; private set; }

    public void UpdateAmount(Money amount)
    {
        if (amount.Amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Budget amount must be non-negative."
            );

        Amount = amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRollover(bool enabled)
    {
        RolloverEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePeriod(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1900);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 2200);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        Year = year;
        Month = month;
        UpdatedAt = DateTime.UtcNow;
    }
}
