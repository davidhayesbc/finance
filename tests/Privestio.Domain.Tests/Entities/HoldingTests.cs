using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class HoldingTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesHolding()
    {
        var account = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0m),
            new DateOnly(2025, 1, 1),
            Guid.NewGuid()
        );

        var holding = new Holding(
            account.Id,
            "xeqt.to",
            "iShares Core Equity ETF Portfolio",
            10.5m,
            new Money(35.12m, "CAD")
        );

        holding.AccountId.Should().Be(account.Id);
        holding.Symbol.Should().Be("XEQT.TO");
        holding.Quantity.Should().Be(10.5m);
        holding.AverageCostPerUnit.Amount.Should().Be(35.12m);
    }

    [Fact]
    public void Constructor_WithNegativeQuantity_Throws()
    {
        var act = () =>
            new Holding(
                Guid.NewGuid(),
                "XEQT.TO",
                "iShares Core Equity ETF Portfolio",
                -1m,
                new Money(35m, "CAD")
            );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
