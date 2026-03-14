using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents an acquisition lot within a holding for cost basis tracking.
/// </summary>
public class Lot : BaseEntity
{
    private Lot() { }

    public Lot(
        Guid holdingId,
        DateOnly acquiredDate,
        decimal quantity,
        Money unitCost,
        string? source = null,
        string? notes = null
    )
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        HoldingId = holdingId;
        AcquiredDate = acquiredDate;
        Quantity = quantity;
        UnitCost = unitCost;
        Source = source?.Trim();
        Notes = notes;
    }

    public Guid HoldingId { get; private set; }
    public Holding? Holding { get; set; }

    public DateOnly AcquiredDate { get; private set; }
    public decimal Quantity { get; private set; }
    public Money UnitCost { get; private set; }
    public string? Source { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        DateOnly acquiredDate,
        decimal quantity,
        Money unitCost,
        string? notes = null
    )
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        AcquiredDate = acquiredDate;
        Quantity = quantity;
        UnitCost = unitCost;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
