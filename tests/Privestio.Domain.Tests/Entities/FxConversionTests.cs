using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class FxConversionTests
{
    [Fact]
    public void Constructor_WithValidArgs_CreatesFxConversion()
    {
        var transactionId = Guid.NewGuid();
        var exchangeRateId = Guid.NewGuid();
        var original = new Money(100m, "CAD");
        var converted = new Money(74m, "USD");

        var fx = new FxConversion(transactionId, original, converted, exchangeRateId, 0.74m);

        fx.TransactionId.Should().Be(transactionId);
        fx.OriginalAmount.Should().Be(original);
        fx.ConvertedAmount.Should().Be(converted);
        fx.ExchangeRateId.Should().Be(exchangeRateId);
        fx.AppliedRate.Should().Be(0.74m);
        fx.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_ZeroAppliedRate_ThrowsArgumentOutOfRangeException()
    {
        var act = () =>
            new FxConversion(
                Guid.NewGuid(),
                new Money(100m, "CAD"),
                new Money(0m, "USD"),
                Guid.NewGuid(),
                0m
            );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeAppliedRate_ThrowsArgumentOutOfRangeException()
    {
        var act = () =>
            new FxConversion(
                Guid.NewGuid(),
                new Money(100m, "CAD"),
                new Money(74m, "USD"),
                Guid.NewGuid(),
                -0.5m
            );

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_SameCurrencies_ThrowsArgumentException()
    {
        var act = () =>
            new FxConversion(
                Guid.NewGuid(),
                new Money(100m, "CAD"),
                new Money(100m, "CAD"),
                Guid.NewGuid(),
                1.0m
            );

        act.Should().Throw<ArgumentException>().WithMessage("*different currencies*");
    }
}
