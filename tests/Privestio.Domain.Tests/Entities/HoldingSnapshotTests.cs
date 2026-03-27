using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class HoldingSnapshotTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesSnapshot()
    {
        var accountId = Guid.NewGuid();
        var securityId = Guid.NewGuid();
        var asOfDate = new DateOnly(2024, 6, 30);

        var snapshot = new HoldingSnapshot(
            accountId,
            securityId,
            "SLGF",
            "Sun Life Growth Fund",
            150.000m,
            new Money(16.50m, "CAD"),
            asOfDate,
            "PDFStatement"
        );

        snapshot.AccountId.Should().Be(accountId);
        snapshot.SecurityId.Should().Be(securityId);
        snapshot.Symbol.Should().Be("SLGF");
        snapshot.SecurityName.Should().Be("Sun Life Growth Fund");
        snapshot.Quantity.Should().Be(150.000m);
        snapshot.UnitPrice.Amount.Should().Be(16.50m);
        snapshot.UnitPrice.CurrencyCode.Should().Be("CAD");
        snapshot.AsOfDate.Should().Be(asOfDate);
        snapshot.Source.Should().Be("PDFStatement");
        snapshot.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithNegativeQuantity_Throws()
    {
        var act = () =>
            new HoldingSnapshot(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "SLGF",
                "Sun Life Growth Fund",
                -1m,
                new Money(16.50m, "CAD"),
                new DateOnly(2024, 6, 30),
                "PDFStatement"
            );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithEmptySymbol_Throws()
    {
        var act = () =>
            new HoldingSnapshot(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "",
                "Sun Life Growth Fund",
                100m,
                new Money(16.50m, "CAD"),
                new DateOnly(2024, 6, 30),
                "PDFStatement"
            );

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptySource_Throws()
    {
        var act = () =>
            new HoldingSnapshot(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "SLGF",
                "Sun Life Growth Fund",
                100m,
                new Money(16.50m, "CAD"),
                new DateOnly(2024, 6, 30),
                ""
            );

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NormalizesSymbolToUppercase()
    {
        var snapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "slgf",
            "Sun Life Growth Fund",
            100m,
            new Money(16.50m, "CAD"),
            new DateOnly(2024, 6, 30),
            "PDFStatement"
        );

        snapshot.Symbol.Should().Be("SLGF");
    }

    [Fact]
    public void MarketValue_ComputedFromQuantityAndUnitPrice()
    {
        var snapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SLGF",
            "Sun Life Growth Fund",
            150.000m,
            new Money(16.50m, "CAD"),
            new DateOnly(2024, 6, 30),
            "PDFStatement"
        );

        snapshot.MarketValue.Amount.Should().Be(2475.00m);
        snapshot.MarketValue.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Constructor_WithZeroQuantity_Allowed()
    {
        var snapshot = new HoldingSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SLGF",
            "Sun Life Growth Fund",
            0m,
            new Money(16.50m, "CAD"),
            new DateOnly(2024, 6, 30),
            "PDFStatement"
        );

        snapshot.Quantity.Should().Be(0m);
        snapshot.MarketValue.Amount.Should().Be(0m);
    }
}
