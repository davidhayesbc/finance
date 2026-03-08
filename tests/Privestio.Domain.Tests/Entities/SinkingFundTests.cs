using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class SinkingFundTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime FutureDate = DateTime.UtcNow.AddMonths(12);

    [Fact]
    public void Constructor_WithValidArgs_CreatesSinkingFund()
    {
        var target = new Money(6000m, "CAD");

        var fund = new SinkingFund(UserId, "Vacation", target, FutureDate);

        fund.Name.Should().Be("Vacation");
        fund.TargetAmount.Should().Be(target);
        fund.DueDate.Should().Be(FutureDate);
        fund.AccumulatedAmount.Amount.Should().Be(0m);
        fund.IsActive.Should().BeTrue();
        fund.AccountId.Should().BeNull();
        fund.CategoryId.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        var act = () => new SinkingFund(UserId, "", new Money(1000m), FutureDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ZeroTarget_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SinkingFund(UserId, "Test", new Money(0m), FutureDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeTarget_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SinkingFund(UserId, "Test", new Money(-500m), FutureDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var fund = new SinkingFund(UserId, "  Vacation  ", new Money(1000m), FutureDate);

        fund.Name.Should().Be("Vacation");
    }

    [Fact]
    public void RecordContribution_IncreasesAccumulatedAmount()
    {
        var fund = new SinkingFund(UserId, "Vacation", new Money(6000m, "CAD"), FutureDate);

        fund.RecordContribution(new Money(500m, "CAD"));

        fund.AccumulatedAmount.Amount.Should().Be(500m);
    }

    [Fact]
    public void RecordContribution_MultipleContributions_Accumulates()
    {
        var fund = new SinkingFund(UserId, "Vacation", new Money(6000m, "CAD"), FutureDate);

        fund.RecordContribution(new Money(500m, "CAD"));
        fund.RecordContribution(new Money(300m, "CAD"));

        fund.AccumulatedAmount.Amount.Should().Be(800m);
    }

    [Fact]
    public void RecordContribution_ZeroAmount_Throws()
    {
        var fund = new SinkingFund(UserId, "Vacation", new Money(6000m, "CAD"), FutureDate);

        var act = () => fund.RecordContribution(new Money(0m, "CAD"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProgressPercentage_ZeroAccumulated_ReturnsZero()
    {
        var fund = new SinkingFund(UserId, "Vacation", new Money(6000m), FutureDate);

        fund.ProgressPercentage.Should().Be(0m);
    }

    [Fact]
    public void ProgressPercentage_HalfAccumulated_Returns50()
    {
        var fund = new SinkingFund(UserId, "Vacation", new Money(6000m, "CAD"), FutureDate);
        fund.RecordContribution(new Money(3000m, "CAD"));

        fund.ProgressPercentage.Should().Be(50m);
    }

    [Fact]
    public void CalculateMonthlySetAside_12MonthsAway_DividesEvenly()
    {
        var dueDate = new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var asOf = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var fund = new SinkingFund(UserId, "Test", new Money(1200m, "CAD"), dueDate);

        var monthly = fund.CalculateMonthlySetAside(asOf);

        monthly.Amount.Should().Be(100m);
        monthly.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void CalculateMonthlySetAside_FullyFunded_ReturnsZero()
    {
        var fund = new SinkingFund(UserId, "Test", new Money(1000m, "CAD"), FutureDate);
        fund.RecordContribution(new Money(1000m, "CAD"));

        var monthly = fund.CalculateMonthlySetAside(DateTime.UtcNow);

        monthly.Amount.Should().Be(0m);
    }

    [Fact]
    public void CalculateMonthlySetAside_PastDue_ReturnsRemainingBalance()
    {
        var pastDate = DateTime.UtcNow.AddMonths(-1);
        var fund = new SinkingFund(UserId, "Test", new Money(1000m, "CAD"), pastDate);

        var monthly = fund.CalculateMonthlySetAside(DateTime.UtcNow);

        monthly.Amount.Should().Be(1000m);
    }

    [Fact]
    public void Rename_ValidName_UpdatesName()
    {
        var fund = new SinkingFund(UserId, "Old", new Money(1000m), FutureDate);

        fund.Rename("New Name");

        fund.Name.Should().Be("New Name");
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        var fund = new SinkingFund(UserId, "Old", new Money(1000m), FutureDate);

        var act = () => fund.Rename("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateTarget_ValidValues_Updates()
    {
        var fund = new SinkingFund(UserId, "Test", new Money(1000m, "CAD"), FutureDate);
        var newDue = FutureDate.AddMonths(6);

        fund.UpdateTarget(new Money(2000m, "CAD"), newDue);

        fund.TargetAmount.Amount.Should().Be(2000m);
        fund.DueDate.Should().Be(newDue);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var fund = new SinkingFund(UserId, "Test", new Money(1000m), FutureDate);

        fund.Deactivate();

        fund.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsActive()
    {
        var fund = new SinkingFund(UserId, "Test", new Money(1000m), FutureDate);
        fund.Deactivate();

        fund.Activate();

        fund.IsActive.Should().BeTrue();
    }
}
