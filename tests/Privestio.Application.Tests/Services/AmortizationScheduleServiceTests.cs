using FluentAssertions;
using Privestio.Application.Services;
using Xunit;

namespace Privestio.Application.Tests.Services;

public class AmortizationScheduleServiceTests
{
    private readonly AmortizationScheduleService _service;

    public AmortizationScheduleServiceTests()
    {
        _service = new AmortizationScheduleService();
    }

    [Fact]
    public void GenerateSchedule_StandardMortgage_ReturnsCorrectEntries()
    {
        // Arrange - $200,000 at 5% for 25 years (300 months), payment ~$1,169.18
        var accountId = Guid.NewGuid();
        var principal = 200000m;
        var annualRate = 5.0m;
        var termMonths = 300;
        var monthlyPayment = 1169.18m;
        var startDate = new DateOnly(2025, 1, 1);

        // Act
        var entries = _service.GenerateSchedule(
            accountId,
            principal,
            annualRate,
            termMonths,
            monthlyPayment,
            startDate,
            "CAD"
        );

        // Assert
        entries.Should().NotBeEmpty();
        entries.First().PaymentNumber.Should().Be(1);
        entries.First().PaymentDate.Should().Be(new DateOnly(2025, 1, 1));
        entries.First().PaymentAmount.Amount.Should().BeGreaterThan(0);
        entries.First().PrincipalAmount.Amount.Should().BeGreaterThan(0);
        entries.First().InterestAmount.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSchedule_FinalPaymentReducesBalanceToZero()
    {
        // Arrange - small loan to verify balance reaches zero
        var accountId = Guid.NewGuid();
        var principal = 10000m;
        var annualRate = 6.0m;
        var termMonths = 12;
        var monthlyPayment = 860.66m;
        var startDate = new DateOnly(2025, 1, 1);

        // Act
        var entries = _service.GenerateSchedule(
            accountId,
            principal,
            annualRate,
            termMonths,
            monthlyPayment,
            startDate,
            "CAD"
        );

        // Assert
        var lastEntry = entries.Last();
        lastEntry.RemainingBalance.Amount.Should().Be(0m);
    }

    [Fact]
    public void GenerateSchedule_InterestPlusPrincipalEqualsPayment()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var principal = 50000m;
        var annualRate = 4.0m;
        var termMonths = 60;
        var monthlyPayment = 921.37m;
        var startDate = new DateOnly(2025, 1, 1);

        // Act
        var entries = _service.GenerateSchedule(
            accountId,
            principal,
            annualRate,
            termMonths,
            monthlyPayment,
            startDate,
            "CAD"
        );

        // Assert - For every entry except the final adjusted one, interest + principal = payment
        foreach (var entry in entries)
        {
            var sum = entry.InterestAmount.Amount + entry.PrincipalAmount.Amount;
            sum.Should()
                .Be(
                    entry.PaymentAmount.Amount,
                    because: $"payment {entry.PaymentNumber}: interest ({entry.InterestAmount.Amount}) + "
                        + $"principal ({entry.PrincipalAmount.Amount}) should equal payment ({entry.PaymentAmount.Amount})"
                );
        }
    }

    [Fact]
    public void GenerateSchedule_BankersRounding_Within1Cent()
    {
        // Arrange - $200,000 at 5% for 25 years (300 months)
        // Standard mortgage payment for this: $1,169.18 per month
        var accountId = Guid.NewGuid();
        var principal = 200000m;
        var annualRate = 5.0m;
        var termMonths = 300;
        var monthlyPayment = 1169.18m;
        var startDate = new DateOnly(2025, 1, 1);

        // Act
        var entries = _service.GenerateSchedule(
            accountId,
            principal,
            annualRate,
            termMonths,
            monthlyPayment,
            startDate,
            "CAD"
        );

        // Assert
        // First payment: interest = 200000 * 0.05/12 = 833.33 (rounded with banker's rounding)
        var firstEntry = entries.First();
        firstEntry.InterestAmount.Amount.Should().BeApproximately(833.33m, 0.01m);

        // Verify the final balance reaches zero or within 1 cent
        var lastEntry = entries.Last();
        lastEntry.RemainingBalance.Amount.Should().BeInRange(0m, 0.01m);

        // All entries should use CAD currency
        entries
            .Should()
            .AllSatisfy(e =>
            {
                e.PaymentAmount.CurrencyCode.Should().Be("CAD");
                e.PrincipalAmount.CurrencyCode.Should().Be("CAD");
                e.InterestAmount.CurrencyCode.Should().Be("CAD");
                e.RemainingBalance.CurrencyCode.Should().Be("CAD");
            });
    }
}
