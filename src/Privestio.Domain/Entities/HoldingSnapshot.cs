using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Point-in-time snapshot of a holding's quantity and price for historical analysis.
/// Each import (e.g. PDF statement) creates snapshots capturing the state of holdings
/// at the statement date, enabling historical portfolio value reconstruction.
/// </summary>
public class HoldingSnapshot : BaseEntity
{
    private HoldingSnapshot() { }

    public HoldingSnapshot(
        Guid accountId,
        Guid securityId,
        string symbol,
        string securityName,
        decimal quantity,
        Money unitPrice,
        DateOnly asOfDate,
        string source
    )
    {
        if (accountId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(accountId));
        if (securityId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(securityId));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        AccountId = accountId;
        SecurityId = securityId;
        Symbol = symbol.ToUpperInvariant().Trim();
        SecurityName = securityName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        MarketValue = new Money(
            Math.Round(quantity * unitPrice.Amount, 2, MidpointRounding.ToEven),
            unitPrice.CurrencyCode
        );
        AsOfDate = asOfDate;
        RecordedAt = DateTime.UtcNow;
        Source = source;
    }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public Guid SecurityId { get; private set; }
    public Security? Security { get; set; }

    public string Symbol { get; private set; } = string.Empty;
    public string SecurityName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money MarketValue { get; private set; }

    /// <summary>The statement/snapshot date these holdings represent.</summary>
    public DateOnly AsOfDate { get; private set; }

    /// <summary>When this snapshot was recorded into the system.</summary>
    public DateTime RecordedAt { get; private set; }

    public string Source { get; private set; } = string.Empty;
}
