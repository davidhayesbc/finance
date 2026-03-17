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
        var security = new Security("XEQT", "XEQT", "iShares Core Equity ETF Portfolio", "CAD");

        var holding = new Holding(
            account.Id,
            security.Id,
            "xeqt",
            security.Name,
            10.5m,
            new Money(35.12m, "CAD")
        );
        holding.RebindSecurity(security);

        holding.AccountId.Should().Be(account.Id);
        holding.SecurityId.Should().Be(security.Id);
        holding.Symbol.Should().Be("XEQT");
        holding.Quantity.Should().Be(10.5m);
        holding.AverageCostPerUnit.Amount.Should().Be(35.12m);
    }

    [Fact]
    public void Constructor_WithNegativeQuantity_Throws()
    {
        var securityId = Guid.NewGuid();

        var act = () =>
            new Holding(
                Guid.NewGuid(),
                securityId,
                "XEQT.TO",
                "iShares Core Equity ETF Portfolio",
                -1m,
                new Money(35m, "CAD")
            );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
