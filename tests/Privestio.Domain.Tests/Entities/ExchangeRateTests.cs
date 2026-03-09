using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class ExchangeRateTests
{
    [Fact]
    public void Constructor_WithValidArgs_CreatesExchangeRate()
    {
        var asOfDate = new DateOnly(2026, 3, 1);
        var rate = new ExchangeRate("CAD", "USD", 0.74m, asOfDate, "Bank of Canada");

        rate.FromCurrency.Should().Be("CAD");
        rate.ToCurrency.Should().Be("USD");
        rate.Rate.Should().Be(0.74m);
        rate.AsOfDate.Should().Be(asOfDate);
        rate.Source.Should().Be("Bank of Canada");
        rate.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        rate.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_EmptyFromCurrency_ThrowsArgumentException()
    {
        var act = () => new ExchangeRate("", "USD", 0.74m, new DateOnly(2026, 3, 1), "Source");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyToCurrency_ThrowsArgumentException()
    {
        var act = () => new ExchangeRate("CAD", "", 0.74m, new DateOnly(2026, 3, 1), "Source");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ZeroRate_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new ExchangeRate("CAD", "USD", 0m, new DateOnly(2026, 3, 1), "Source");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeRate_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new ExchangeRate("CAD", "USD", -0.5m, new DateOnly(2026, 3, 1), "Source");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_SameCurrencies_ThrowsArgumentException()
    {
        var act = () => new ExchangeRate("CAD", "CAD", 1.0m, new DateOnly(2026, 3, 1), "Source");

        act.Should().Throw<ArgumentException>().WithMessage("*same*");
    }

    [Fact]
    public void Constructor_UppercasesCurrencyCodes()
    {
        var rate = new ExchangeRate("cad", "usd", 0.74m, new DateOnly(2026, 3, 1), "Source");

        rate.FromCurrency.Should().Be("CAD");
        rate.ToCurrency.Should().Be("USD");
    }

    [Fact]
    public void Convert_FromCurrencyToToCurrency_AppliesRate()
    {
        var rate = new ExchangeRate("CAD", "USD", 0.74m, new DateOnly(2026, 3, 1), "Source");
        var cadAmount = new Money(100m, "CAD");

        var result = rate.Convert(cadAmount);

        result.Amount.Should().Be(74.00m);
        result.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Convert_WrongCurrency_ThrowsInvalidOperationException()
    {
        var rate = new ExchangeRate("CAD", "USD", 0.74m, new DateOnly(2026, 3, 1), "Source");
        var eurAmount = new Money(100m, "EUR");

        var act = () => rate.Convert(eurAmount);

        act.Should().Throw<InvalidOperationException>();
    }
}
