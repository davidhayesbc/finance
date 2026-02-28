using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithAmountAndCurrency_CreatesInstance()
    {
        var money = new Money(100.00m, "CAD");

        money.Amount.Should().Be(100.00m);
        money.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Constructor_DefaultCurrency_IsCAD()
    {
        var money = new Money(50.00m);

        money.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var a = new Money(100.00m, "CAD");
        var b = new Money(50.00m, "CAD");

        var result = a + b;

        result.Amount.Should().Be(150.00m);
        result.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Add_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var cad = new Money(100.00m, "CAD");
        var usd = new Money(50.00m, "USD");

        var act = () => cad + usd;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var a = new Money(100.00m, "CAD");
        var b = new Money(30.00m, "CAD");

        var result = a - b;

        result.Amount.Should().Be(70.00m);
        result.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Subtract_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var cad = new Money(100.00m, "CAD");
        var usd = new Money(50.00m, "USD");

        var act = () => cad - usd;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Negate_ReturnsNegativeAmount()
    {
        var money = new Money(100.00m, "CAD");

        var result = -money;

        result.Amount.Should().Be(-100.00m);
        result.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Abs_NegativeAmount_ReturnsPositiveAmount()
    {
        var money = new Money(-50.00m, "CAD");

        var result = money.Abs();

        result.Amount.Should().Be(50.00m);
    }

    [Fact]
    public void Zero_ReturnsZeroWithCurrency()
    {
        var zero = Money.Zero("USD");

        zero.Amount.Should().Be(0m);
        zero.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void GreaterThan_SameCurrency_ComparesCorrectly()
    {
        var large = new Money(100.00m, "CAD");
        var small = new Money(50.00m, "CAD");

        (large > small).Should().BeTrue();
        (small > large).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var cad = new Money(100.00m, "CAD");
        var usd = new Money(50.00m, "USD");

        var act = () => cad > usd;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = new Money(100.00m, "CAD");
        var b = new Money(100.00m, "CAD");

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        var a = new Money(100.00m, "CAD");
        var b = new Money(200.00m, "CAD");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var money = new Money(1234.56m, "CAD");

        money.ToString().Should().Be("1234.56 CAD");
    }
}
