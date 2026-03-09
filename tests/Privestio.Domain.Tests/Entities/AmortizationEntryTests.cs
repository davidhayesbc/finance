using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class AmortizationEntryTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesEntry()
    {
        var entry = new AmortizationEntry(
            AccountId,
            1,
            new DateOnly(2026, 4, 1),
            new Money(1500m, "CAD"),
            new Money(800m, "CAD"),
            new Money(700m, "CAD"),
            new Money(199200m, "CAD")
        );

        entry.AccountId.Should().Be(AccountId);
        entry.PaymentNumber.Should().Be(1);
        entry.PaymentDate.Should().Be(new DateOnly(2026, 4, 1));
        entry.PaymentAmount.Amount.Should().Be(1500m);
        entry.PrincipalAmount.Amount.Should().Be(800m);
        entry.InterestAmount.Amount.Should().Be(700m);
        entry.RemainingBalance.Amount.Should().Be(199200m);
        entry.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_PaymentNumberZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () =>
            new AmortizationEntry(
                AccountId,
                0,
                new DateOnly(2026, 4, 1),
                new Money(1500m),
                new Money(800m),
                new Money(700m),
                new Money(199200m)
            );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativePaymentAmount_ThrowsArgumentOutOfRangeException()
    {
        var act = () =>
            new AmortizationEntry(
                AccountId,
                1,
                new DateOnly(2026, 4, 1),
                new Money(-1500m),
                new Money(800m),
                new Money(700m),
                new Money(199200m)
            );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PrincipalPlusInterest_EqualsPaymentAmount()
    {
        var entry = new AmortizationEntry(
            AccountId,
            1,
            new DateOnly(2026, 4, 1),
            new Money(1500m, "CAD"),
            new Money(800m, "CAD"),
            new Money(700m, "CAD"),
            new Money(199200m, "CAD")
        );

        var sum = entry.PrincipalAmount.Amount + entry.InterestAmount.Amount;
        sum.Should().Be(entry.PaymentAmount.Amount);
    }
}
