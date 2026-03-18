using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
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
    public void AddOrUpdateAlias_WithSourceAndExchange_PrefersExchangeSpecificPrimary()
    {
        var security = new Security("XEQT", "XEQT", "iShares Core Equity ETF Portfolio", "CAD");

        security.AddOrUpdateAlias("XEQT", "Wealthsimple", false, "XTSE");
        security.AddOrUpdateAlias("XEQT.TO", "Wealthsimple", true, "XTSE");

        security.GetPreferredSymbol("Wealthsimple", "XTSE").Should().Be("XEQT.TO");
    }

    [Fact]
    public void AddOrUpdateIdentifier_WithCusip_IsResolvable()
    {
        var security = new Security("XEQT", "XEQT", "iShares Core Equity ETF Portfolio", "CAD");

        security.AddOrUpdateIdentifier(SecurityIdentifierType.Cusip, "46436D108", true);

        security.HasIdentifier(SecurityIdentifierType.Cusip, "46436D108").Should().BeTrue();
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
