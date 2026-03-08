using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// A recurring income or expense pattern used for forecasting and auto-generation.
/// </summary>
public class RecurringTransaction : BaseEntity
{
    private RecurringTransaction() { }

    public RecurringTransaction(
        Guid userId,
        Guid accountId,
        string description,
        Money amount,
        TransactionType transactionType,
        RecurrenceFrequency frequency,
        DateTime startDate,
        DateTime? endDate = null,
        Guid? categoryId = null,
        Guid? payeeId = null,
        string? notes = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (endDate.HasValue && endDate.Value < startDate)
            throw new ArgumentException("End date must not be before start date.", nameof(endDate));

        UserId = userId;
        AccountId = accountId;
        Description = description.Trim();
        Amount = amount;
        TransactionType = transactionType;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        CategoryId = categoryId;
        PayeeId = payeeId;
        Notes = notes;
        IsActive = true;
        NextOccurrence = startDate;
    }

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public string Description { get; private set; } = string.Empty;
    public Money Amount { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public RecurrenceFrequency Frequency { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime NextOccurrence { get; private set; }
    public DateTime? LastGenerated { get; private set; }

    public Guid? CategoryId { get; private set; }
    public Category? Category { get; set; }

    public Guid? PayeeId { get; private set; }
    public Payee? Payee { get; set; }

    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// Advances the NextOccurrence to the following recurrence date.
    /// </summary>
    public void AdvanceToNextOccurrence()
    {
        LastGenerated = NextOccurrence;
        NextOccurrence = CalculateNextDate(NextOccurrence);

        if (EndDate.HasValue && NextOccurrence > EndDate.Value)
        {
            IsActive = false;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates projected occurrence dates from the current NextOccurrence up to the given horizon.
    /// </summary>
    public IReadOnlyList<DateTime> ProjectOccurrences(DateTime horizon)
    {
        var dates = new List<DateTime>();
        var current = NextOccurrence;
        var effectiveEnd = EndDate.HasValue && EndDate.Value < horizon ? EndDate.Value : horizon;

        while (current <= effectiveEnd)
        {
            dates.Add(current);
            current = CalculateNextDate(current);
        }

        return dates.AsReadOnly();
    }

    public void Disable()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string description,
        Money amount,
        TransactionType transactionType,
        RecurrenceFrequency frequency,
        Guid? categoryId,
        Guid? payeeId,
        string? notes
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Description = description.Trim();
        Amount = amount;
        TransactionType = transactionType;
        Frequency = frequency;
        CategoryId = categoryId;
        PayeeId = payeeId;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSchedule(DateTime startDate, DateTime? endDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
            throw new ArgumentException("End date must not be before start date.", nameof(endDate));

        StartDate = startDate;
        EndDate = endDate;
        NextOccurrence = startDate;
        LastGenerated = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private DateTime CalculateNextDate(DateTime from) =>
        Frequency switch
        {
            RecurrenceFrequency.Weekly => from.AddDays(7),
            RecurrenceFrequency.BiWeekly => from.AddDays(14),
            RecurrenceFrequency.Monthly => from.AddMonths(1),
            RecurrenceFrequency.Quarterly => from.AddMonths(3),
            RecurrenceFrequency.SemiAnnually => from.AddMonths(6),
            RecurrenceFrequency.Annually => from.AddYears(1),
            _ => from.AddMonths(1),
        };
}
