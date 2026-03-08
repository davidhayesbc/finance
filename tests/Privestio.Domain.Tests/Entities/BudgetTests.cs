using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class BudgetTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesBudget()
    {
        var amount = new Money(500m, "CAD");

        var budget = new Budget(UserId, CategoryId, 2026, 3, amount);

        budget.UserId.Should().Be(UserId);
        budget.CategoryId.Should().Be(CategoryId);
        budget.Year.Should().Be(2026);
        budget.Month.Should().Be(3);
        budget.Amount.Should().Be(amount);
        budget.RolloverEnabled.Should().BeFalse();
        budget.Notes.Should().BeNull();
        budget.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithRolloverAndNotes_SetsProperties()
    {
        var budget = new Budget(
            UserId,
            CategoryId,
            2026,
            1,
            new Money(100m),
            true,
            "Monthly groceries"
        );

        budget.RolloverEnabled.Should().BeTrue();
        budget.Notes.Should().Be("Monthly groceries");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Constructor_InvalidMonth_ThrowsArgumentOutOfRangeException(int month)
    {
        var act = () => new Budget(UserId, CategoryId, 2026, month, new Money(100m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2201)]
    public void Constructor_InvalidYear_ThrowsArgumentOutOfRangeException(int year)
    {
        var act = () => new Budget(UserId, CategoryId, year, 1, new Money(100m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new Budget(UserId, CategoryId, 2026, 1, new Money(-50m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ZeroAmount_IsAllowed()
    {
        var budget = new Budget(UserId, CategoryId, 2026, 1, new Money(0m));

        budget.Amount.Amount.Should().Be(0m);
    }

    [Fact]
    public void UpdateAmount_ValidAmount_Updates()
    {
        var budget = new Budget(UserId, CategoryId, 2026, 1, new Money(500m));
        var before = budget.UpdatedAt;

        budget.UpdateAmount(new Money(750m));

        budget.Amount.Amount.Should().Be(750m);
        budget.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateAmount_NegativeAmount_Throws()
    {
        var budget = new Budget(UserId, CategoryId, 2026, 1, new Money(500m));

        var act = () => budget.UpdateAmount(new Money(-10m));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetRollover_ChangesValue()
    {
        var budget = new Budget(UserId, CategoryId, 2026, 1, new Money(500m));

        budget.SetRollover(true);

        budget.RolloverEnabled.Should().BeTrue();
    }

    [Fact]
    public void UpdateNotes_SetsNotes()
    {
        var budget = new Budget(UserId, CategoryId, 2026, 1, new Money(500m));

        budget.UpdateNotes("New note");

        budget.Notes.Should().Be("New note");
    }

    [Fact]
    public void UpdatePeriod_ValidPeriod_Updates()
    {
        var budget = new Budget(UserId, CategoryId, 2026, 1, new Money(500m));

        budget.UpdatePeriod(2026, 6);

        budget.Year.Should().Be(2026);
        budget.Month.Should().Be(6);
    }

    [Fact]
    public void UpdatePeriod_InvalidMonth_Throws()
    {
        var budget = new Budget(UserId, CategoryId, 2026, 1, new Money(500m));

        var act = () => budget.UpdatePeriod(2026, 13);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
