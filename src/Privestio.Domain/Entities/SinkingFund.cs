using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// A savings target for a lump-sum expense. Tracks progress toward a target amount by a due date.
/// </summary>
public class SinkingFund : BaseEntity
{
    private SinkingFund() { }

    public SinkingFund(
        Guid userId,
        string name,
        Money targetAmount,
        DateTime dueDate,
        Guid? accountId = null,
        Guid? categoryId = null,
        string? notes = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (targetAmount.Amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(targetAmount),
                "Target amount must be positive."
            );

        UserId = userId;
        Name = name.Trim();
        TargetAmount = targetAmount;
        DueDate = dueDate;
        AccountId = accountId;
        CategoryId = categoryId;
        Notes = notes;
        AccumulatedAmount = Money.Zero(targetAmount.CurrencyCode);
        IsActive = true;
    }

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public string Name { get; private set; } = string.Empty;

    public Money TargetAmount { get; private set; }
    public Money AccumulatedAmount { get; private set; }
    public DateTime DueDate { get; private set; }

    public Guid? AccountId { get; private set; }
    public Account? Account { get; set; }

    public Guid? CategoryId { get; private set; }
    public Category? Category { get; set; }

    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// Calculates the monthly set-aside amount needed to reach the target by the due date.
    /// </summary>
    public Money CalculateMonthlySetAside(DateTime asOfDate)
    {
        var remaining = TargetAmount.Amount - AccumulatedAmount.Amount;
        if (remaining <= 0)
            return Money.Zero(TargetAmount.CurrencyCode);

        var monthsRemaining =
            ((DueDate.Year - asOfDate.Year) * 12) + DueDate.Month - asOfDate.Month;
        if (monthsRemaining <= 0)
            return new Money(remaining, TargetAmount.CurrencyCode);

        var monthly = Math.Round(remaining / monthsRemaining, 2, MidpointRounding.ToEven);
        return new Money(monthly, TargetAmount.CurrencyCode);
    }

    /// <summary>
    /// Progress toward the target as a percentage (0-100).
    /// </summary>
    public decimal ProgressPercentage =>
        TargetAmount.Amount > 0
            ? Math.Round(
                AccumulatedAmount.Amount / TargetAmount.Amount * 100,
                2,
                MidpointRounding.ToEven
            )
            : 0m;

    /// <summary>
    /// Whether the fund is on track based on time elapsed and amount accumulated.
    /// </summary>
    public bool IsOnTrack(DateTime asOfDate)
    {
        var totalMonths = ((DueDate.Year - CreatedAt.Year) * 12) + DueDate.Month - CreatedAt.Month;
        if (totalMonths <= 0)
            return AccumulatedAmount.Amount >= TargetAmount.Amount;

        var elapsedMonths =
            ((asOfDate.Year - CreatedAt.Year) * 12) + asOfDate.Month - CreatedAt.Month;
        var expectedProgress = (decimal)elapsedMonths / totalMonths;
        var expectedAmount = TargetAmount.Amount * expectedProgress;

        return AccumulatedAmount.Amount >= expectedAmount;
    }

    public void RecordContribution(Money amount)
    {
        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Contribution must be positive.");

        AccumulatedAmount = AccumulatedAmount.Add(amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTarget(Money targetAmount, DateTime dueDate)
    {
        if (targetAmount.Amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(targetAmount),
                "Target amount must be positive."
            );

        TargetAmount = targetAmount;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
