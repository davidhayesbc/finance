using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// A single entry in an amortization schedule for a mortgage or loan.
/// </summary>
public class AmortizationEntry : BaseEntity
{
    private AmortizationEntry() { }

    public AmortizationEntry(
        Guid accountId,
        int paymentNumber,
        DateOnly paymentDate,
        Money paymentAmount,
        Money principalAmount,
        Money interestAmount,
        Money remainingBalance
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(paymentNumber, 1);

        if (paymentAmount.Amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(paymentAmount),
                "Payment amount must be non-negative."
            );

        AccountId = accountId;
        PaymentNumber = paymentNumber;
        PaymentDate = paymentDate;
        PaymentAmount = paymentAmount;
        PrincipalAmount = principalAmount;
        InterestAmount = interestAmount;
        RemainingBalance = remainingBalance;
    }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public int PaymentNumber { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public Money PaymentAmount { get; private set; }
    public Money PrincipalAmount { get; private set; }
    public Money InterestAmount { get; private set; }
    public Money RemainingBalance { get; private set; }
}
