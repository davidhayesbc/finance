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

    [Fact]
    public void Rename_WhenNameUnchanged_DoesNotUpdateTimestamp()
    {
        var security = new Security("TEST", "TEST", "Original Name", "CAD");
        var originalUpdatedAt = security.UpdatedAt;

        security.Rename("Original Name");

        security.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Rename_WhenNameChanged_UpdatesNameAndTimestamp()
    {
        var security = new Security("TEST", "TEST", "Original Name", "CAD");
        var originalUpdatedAt = security.UpdatedAt;

        security.Rename("New Name");

        security.Name.Should().Be("New Name");
        security.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDisplaySymbol_WhenSymbolUnchanged_DoesNotModifyAliases()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");
        var originalAliasCount = security.Aliases.Count;
        var originalUpdatedAt = security.UpdatedAt;

        security.UpdateDisplaySymbol("TEST");

        security.Aliases.Should().HaveCount(originalAliasCount);
        security.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDisplaySymbol_WhenSymbolChanged_UpdatesSymbolAndCreatesAlias()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");

        security.UpdateDisplaySymbol("NEWTEST");

        security.DisplaySymbol.Should().Be("NEWTEST");
        security.Aliases.Should().Contain(a => a.Symbol == "NEWTEST" && a.IsPrimary);
    }

    [Fact]
    public void UpdateCurrency_WhenSameValue_DoesNotUpdateTimestamp()
    {
        var security = new Security("TEST", "TEST", "Test", "CAD");
        var originalUpdatedAt = security.UpdatedAt;

        security.UpdateCurrency("CAD");

        security.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void UpdateExchange_WhenSameValue_DoesNotUpdateTimestamp()
    {
        var security = new Security("TEST", "TEST", "Test", "CAD", "TSX");
        var originalUpdatedAt = security.UpdatedAt;

        security.UpdateExchange("TSX");

        security.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void SetCashEquivalent_WhenSameValue_DoesNotUpdateTimestamp()
    {
        var security = new Security("TEST", "TEST", "Test", "CAD");
        var originalUpdatedAt = security.UpdatedAt;

        security.SetCashEquivalent(false);

        security.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void SetCashEquivalent_WhenValueChanged_Updates()
    {
        var security = new Security("TEST", "TEST", "Test", "CAD");

        security.SetCashEquivalent(true);

        security.IsCashEquivalent.Should().BeTrue();
    }

    [Fact]
    public void ClearPrimary_WhenAliasAlreadyNotPrimary_DoesNotUpdateTimestamp()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");
        // Add a non-primary alias
        security.AddOrUpdateAlias("OTHER", "SomeSource", false);
        var nonPrimaryAlias = security.Aliases.First(a => a.Symbol == "OTHER");
        var originalUpdatedAt = nonPrimaryAlias.UpdatedAt;

        // ClearPrimary is internal, but we can trigger it through AddOrUpdateAlias with isPrimary=true
        // for a different alias in the same source scope — this should NOT modify the non-primary alias
        security.AddOrUpdateAlias("ANOTHER", "SomeSource", true);

        // The non-primary "OTHER" alias should not have its UpdatedAt changed
        nonPrimaryAlias.UpdatedAt.Should().Be(originalUpdatedAt);
    }
}
