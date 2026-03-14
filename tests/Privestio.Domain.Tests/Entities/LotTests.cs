using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class LotTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesLot()
    {
        var lot = new Lot(
            Guid.NewGuid(),
            new DateOnly(2025, 2, 3),
            2.1361m,
            new Money(35.1098m, "CAD"),
            "Trade",
            "Initial buy"
        );

        lot.Quantity.Should().Be(2.1361m);
        lot.UnitCost.Amount.Should().Be(35.1098m);
        lot.Source.Should().Be("Trade");
    }

    [Fact]
    public void Constructor_WithZeroQuantity_Throws()
    {
        var act = () =>
            new Lot(Guid.NewGuid(), new DateOnly(2025, 2, 3), 0m, new Money(35m, "CAD"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
