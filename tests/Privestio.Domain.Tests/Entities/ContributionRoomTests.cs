using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class ContributionRoomTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesContributionRoom()
    {
        var annualLimit = new Money(7000m, "CAD");
        var carryForward = new Money(20000m, "CAD");

        var room = new ContributionRoom(AccountId, 2026, annualLimit, carryForward);

        room.AccountId.Should().Be(AccountId);
        room.Year.Should().Be(2026);
        room.AnnualLimit.Should().Be(annualLimit);
        room.CarryForwardRoom.Should().Be(carryForward);
        room.ContributionsYtd.Amount.Should().Be(0m);
        room.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_NegativeAnnualLimit_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new ContributionRoom(AccountId, 2026, new Money(-1m), new Money(0m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2201)]
    public void Constructor_InvalidYear_ThrowsArgumentOutOfRangeException(int year)
    {
        var act = () => new ContributionRoom(AccountId, year, new Money(7000m), new Money(0m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemainingRoom_ReturnsCorrectCalculation()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(20000m, "CAD")
        );

        room.RemainingRoom.Amount.Should().Be(27000m);
    }

    [Fact]
    public void RecordContribution_ValidAmount_IncreasesYtd()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        room.RecordContribution(new Money(2500m, "CAD"));

        room.ContributionsYtd.Amount.Should().Be(2500m);
        room.RemainingRoom.Amount.Should().Be(4500m);
    }

    [Fact]
    public void RecordContribution_MultipleContributions_Accumulates()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        room.RecordContribution(new Money(1000m, "CAD"));
        room.RecordContribution(new Money(2000m, "CAD"));

        room.ContributionsYtd.Amount.Should().Be(3000m);
    }

    [Fact]
    public void RecordContribution_ZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        var act = () => room.RecordContribution(new Money(0m, "CAD"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RecordContribution_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        var act = () => room.RecordContribution(new Money(-500m, "CAD"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateAnnualLimit_ValidAmount_Updates()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        room.UpdateAnnualLimit(new Money(7500m, "CAD"));

        room.AnnualLimit.Amount.Should().Be(7500m);
    }

    [Fact]
    public void UpdateAnnualLimit_NegativeAmount_Throws()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        var act = () => room.UpdateAnnualLimit(new Money(-1m, "CAD"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetCarryForward_ValidAmount_Updates()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        room.SetCarryForward(new Money(15000m, "CAD"));

        room.CarryForwardRoom.Amount.Should().Be(15000m);
    }

    [Fact]
    public void SetCarryForward_NegativeAmount_Throws()
    {
        var room = new ContributionRoom(
            AccountId,
            2026,
            new Money(7000m, "CAD"),
            new Money(0m, "CAD")
        );

        var act = () => room.SetCarryForward(new Money(-1m, "CAD"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
