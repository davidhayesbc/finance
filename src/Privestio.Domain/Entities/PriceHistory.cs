using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Point-in-time security price record.
/// AsOfDate is the market date the price represents.
/// RecordedAt is when the price was fetched/stored (for stale-price detection).
/// </summary>
public class PriceHistory : BaseEntity
{
    private PriceHistory() { }

    public PriceHistory(
        string symbol,
        Money price,
        DateOnly asOfDate,
        string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        Symbol = symbol.ToUpperInvariant().Trim();
        Price = price;
        AsOfDate = asOfDate;
        RecordedAt = DateTime.UtcNow;
        Source = source;
    }

    public string Symbol { get; private set; } = string.Empty;
    public Money Price { get; private set; }

    /// <summary>The market date this price applies to.</summary>
    public DateOnly AsOfDate { get; private set; }

    /// <summary>When this price record was fetched/stored.</summary>
    public DateTime RecordedAt { get; private set; }

    public string Source { get; private set; } = string.Empty;
}
