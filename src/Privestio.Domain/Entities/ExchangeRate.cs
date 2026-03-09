using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// An exchange rate between two currencies at a point in time.
/// Uses dual-date model: AsOfDate (market date) + RecordedAt (fetch time).
/// </summary>
public class ExchangeRate : BaseEntity
{
    private ExchangeRate() { }

    public ExchangeRate(
        string fromCurrency,
        string toCurrency,
        decimal rate,
        DateOnly asOfDate,
        string source
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(toCurrency);

        fromCurrency = fromCurrency.Trim().ToUpperInvariant();
        toCurrency = toCurrency.Trim().ToUpperInvariant();

        if (fromCurrency == toCurrency)
            throw new ArgumentException(
                "From and To currencies cannot be the same.",
                nameof(toCurrency)
            );

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate);

        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        Rate = rate;
        AsOfDate = asOfDate;
        RecordedAt = DateTime.UtcNow;
        Source = source;
    }

    public string FromCurrency { get; private set; } = string.Empty;
    public string ToCurrency { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public DateOnly AsOfDate { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// Converts a Money amount from FromCurrency to ToCurrency using this rate.
    /// </summary>
    public Money Convert(Money amount)
    {
        if (amount.CurrencyCode != FromCurrency)
            throw new InvalidOperationException(
                $"Cannot convert {amount.CurrencyCode} using a {FromCurrency}/{ToCurrency} rate."
            );

        var converted = Math.Round(amount.Amount * Rate, 2, MidpointRounding.ToEven);
        return new Money(converted, ToCurrency);
    }
}
