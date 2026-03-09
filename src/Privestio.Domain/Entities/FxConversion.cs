using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Records the foreign exchange conversion applied to a cross-currency transaction.
/// Links a transaction to an exchange rate with the actual applied rate and converted amounts.
/// </summary>
public class FxConversion : BaseEntity
{
    private FxConversion() { }

    public FxConversion(
        Guid transactionId,
        Money originalAmount,
        Money convertedAmount,
        Guid exchangeRateId,
        decimal appliedRate
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(appliedRate);

        if (originalAmount.CurrencyCode == convertedAmount.CurrencyCode)
            throw new ArgumentException(
                "Original and converted amounts must be in different currencies.",
                nameof(convertedAmount)
            );

        TransactionId = transactionId;
        OriginalAmount = originalAmount;
        ConvertedAmount = convertedAmount;
        ExchangeRateId = exchangeRateId;
        AppliedRate = appliedRate;
    }

    public Guid TransactionId { get; private set; }
    public Transaction? Transaction { get; set; }

    public Money OriginalAmount { get; private set; }
    public Money ConvertedAmount { get; private set; }

    public Guid ExchangeRateId { get; private set; }
    public ExchangeRate? ExchangeRate { get; set; }

    public decimal AppliedRate { get; private set; }
}
