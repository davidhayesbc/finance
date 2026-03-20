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
        security.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void AddOrUpdateAlias_WithProviderPrimary_PrefersProviderSymbol()
    {
        var security = new Security("KILO.B", "KILO.B", "Kilo Security", "CAD");

        security.AddOrUpdateAlias("KILO-B.TO", "YahooFinance", true);

        security.GetPreferredSymbol("YahooFinance").Should().Be("KILO-B.TO");
        security.GetPreferredSymbol().Should().Be("KILO.B"); // Falls back to DisplaySymbol
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
    public void UpdateDisplaySymbol_WhenSymbolUnchanged_DoesNotModifyAnything()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");
        var originalUpdatedAt = security.UpdatedAt;

        security.UpdateDisplaySymbol("TEST");

        security.Aliases.Should().BeEmpty();
        security.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDisplaySymbol_WhenSymbolChanged_UpdatesSymbolOnly()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");

        security.UpdateDisplaySymbol("NEWTEST");

        security.DisplaySymbol.Should().Be("NEWTEST");
        security.Aliases.Should().BeEmpty();
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

    [Fact]
    public void AddOrUpdateAlias_WithNullSource_Throws()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");

        var act = () => security.AddOrUpdateAlias("TEST.TO", null!, true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddOrUpdateAlias_WithEmptySource_Throws()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");

        var act = () => security.AddOrUpdateAlias("TEST.TO", " ", true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveAlias_AllowsRemovingAnyAlias()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");
        security.AddOrUpdateAlias("TEST.TO", "YahooFinance", true);
        var alias = security.Aliases.First();

        var result = security.RemoveAlias(alias.Id);

        result.Should().BeTrue();
        security.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void UpdateAlias_AllowsEditingAnyAlias()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");
        security.AddOrUpdateAlias("TEST.TO", "YahooFinance", true);
        var alias = security.Aliases.First();

        var updated = security.UpdateAlias(alias.Id, "TEST-NEW.TO", "YahooFinance", "XTSE", false);

        updated.Symbol.Should().Be("TEST-NEW.TO");
        updated.Exchange.Should().Be("XTSE");
    }

    [Fact]
    public void SetPricingProviderOrder_WithValidList_SetsProperty()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");

        security.SetPricingProviderOrder(["MsnFinance", "YahooFinance"]);

        security.PricingProviderOrder.Should().BeEquivalentTo(["MsnFinance", "YahooFinance"]);
    }

    [Fact]
    public void SetPricingProviderOrder_WithNull_ClearsOverride()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");
        security.SetPricingProviderOrder(["YahooFinance"]);

        security.SetPricingProviderOrder(null);

        security.PricingProviderOrder.Should().BeNull();
    }

    [Fact]
    public void SetPricingProviderOrder_WithBlankEntry_Throws()
    {
        var security = new Security("TEST", "TEST", "Test Security", "CAD");

        var act = () => security.SetPricingProviderOrder(["YahooFinance", " "]);

        act.Should().Throw<ArgumentException>();
    }
}
