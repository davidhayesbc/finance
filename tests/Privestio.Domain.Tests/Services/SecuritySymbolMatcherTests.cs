using Privestio.Domain.Services;

namespace Privestio.Domain.Tests.Services;

public class SecuritySymbolMatcherTests
{
    [Fact]
    public void Normalize_UppercasesAndTrims()
    {
        var normalized = SecuritySymbolMatcher.Normalize("  xeqt.to  ");

        normalized.Should().Be("XEQT.TO");
    }

    [Fact]
    public void GetLookupCandidates_WithoutExchangeSuffix_AddsPrimaryExchangeAlias()
    {
        var candidates = SecuritySymbolMatcher.GetLookupCandidates("XEQT");

        candidates.Should().ContainInOrder("XEQT", "XEQT.TO");
    }

    [Fact]
    public void GetLookupCandidates_WithExchangeSuffix_AddsBaseSymbolAlias()
    {
        var candidates = SecuritySymbolMatcher.GetLookupCandidates("XEQT.TO");

        candidates.Should().ContainInOrder("XEQT.TO", "XEQT");
    }
}
