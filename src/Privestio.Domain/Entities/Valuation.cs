using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Manual asset/property valuation record.
/// EffectiveDate is when the valuation applies (may be back-dated).
/// RecordedAt is when the valuation was entered into the system.
/// Account.CurrentBalance for Property accounts is derived from the latest Valuation by EffectiveDate.
/// </summary>
public class Valuation : BaseEntity
{
    private Valuation() { }

    public Valuation(
        Guid accountId,
        Money estimatedValue,
        DateOnly effectiveDate,
        string source,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        AccountId = accountId;
        EstimatedValue = estimatedValue;
        EffectiveDate = effectiveDate;
        RecordedAt = DateTime.UtcNow;
        Source = source;
        Notes = notes;
    }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public Money EstimatedValue { get; private set; }

    /// <summary>The date this valuation applies to (may be back-dated).</summary>
    public DateOnly EffectiveDate { get; private set; }

    /// <summary>When this valuation record was entered into the system.</summary>
    public DateTime RecordedAt { get; private set; }

    public string Source { get; private set; } = string.Empty;
    public string? Notes { get; set; }

    public void UpdateValue(Money estimatedValue)
    {
        EstimatedValue = estimatedValue;
        UpdatedAt = DateTime.UtcNow;
    }
}
