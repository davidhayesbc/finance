using Privestio.Domain.Entities;
using Privestio.Domain.Services;

namespace Privestio.Domain.Tests.Entities;

public class SecurityTests
{
    [Fact]
    public void Constructor_NormalizesCanonicalAndDisplaySymbols()
    {
        var security = new Security("  kilo.b ", " kilo.b ", "Kilo Security", "cad");

        security.CanonicalSymbol.Should().Be("KILO.B");
        security.DisplaySymbol.Should().Be("KILO.B");
        security.Currency.Should().Be("CAD");
        security.Aliases.Should().ContainSingle(a => a.Symbol == "KILO.B" && a.Source == null);
    }

    [Fact]
    public void AddOrUpdateAlias_WithProviderPrimary_PrefersProviderSymbol()
    {
        var security = new Security("KILO.B", "KILO.B", "Kilo Security", "CAD");

        security.AddOrUpdateAlias("KILO-B.TO", "YahooFinance", true);

        security.GetPreferredSymbol("YahooFinance").Should().Be("KILO-B.TO");
        security.GetPreferredSymbol().Should().Be("KILO.B");
    }

    [Fact]
    public void MarkCashEquivalent_SetsFlag()
    {
        var security = new Security("CASH.TO", "CASH.TO", "Cash ETF", "CAD");

        security.MarkCashEquivalent();

        security.IsCashEquivalent.Should().BeTrue();
    }

    [Fact]
    public void Normalize_UppercasesAndTrims()
    {
        var normalized = SecuritySymbolMatcher.Normalize("  zuag.f  ");

        normalized.Should().Be("ZUAG.F");
    }
}
