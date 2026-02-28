using Privestio.Domain.Entities;

namespace Privestio.Domain.Tests.Entities;

public class PayeeTests
{
    private static Guid _ownerId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_CreatesPayee()
    {
        var payee = new Payee("Amazon", _ownerId);

        payee.DisplayName.Should().Be("Amazon");
        payee.OwnerId.Should().Be(_ownerId);
        payee.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void AddAlias_AddsAliasToList()
    {
        var payee = new Payee("Amazon", _ownerId);

        payee.AddAlias("AMZN*1A2B3C");

        payee.Aliases.Should().Contain("AMZN*1A2B3C");
    }

    [Fact]
    public void AddAlias_DuplicateAlias_DoesNotAddTwice()
    {
        var payee = new Payee("Amazon", _ownerId);

        payee.AddAlias("AMZN*1A2B3C");
        payee.AddAlias("AMZN*1A2B3C");

        payee.Aliases.Should().HaveCount(1);
    }

    [Fact]
    public void AddAlias_CaseInsensitiveDuplicate_DoesNotAddTwice()
    {
        var payee = new Payee("Amazon", _ownerId);

        payee.AddAlias("amzn*1a2b3c");
        payee.AddAlias("AMZN*1A2B3C");

        payee.Aliases.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveAlias_ExistingAlias_RemovesIt()
    {
        var payee = new Payee("Amazon", _ownerId);
        payee.AddAlias("AMZN*1A2B3C");

        payee.RemoveAlias("AMZN*1A2B3C");

        payee.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void MatchesAlias_RawPayeeMatchesAlias_ReturnsTrue()
    {
        var payee = new Payee("Amazon", _ownerId);
        payee.AddAlias("AMZN*1A2B3C");

        payee.MatchesAlias("AMZN*1A2B3C").Should().BeTrue();
    }

    [Fact]
    public void MatchesAlias_RawPayeeMatchesDisplayName_ReturnsTrue()
    {
        var payee = new Payee("Amazon", _ownerId);

        payee.MatchesAlias("Amazon").Should().BeTrue();
    }

    [Fact]
    public void MatchesAlias_NoMatch_ReturnsFalse()
    {
        var payee = new Payee("Amazon", _ownerId);

        payee.MatchesAlias("Walmart").Should().BeFalse();
    }

    [Fact]
    public void MatchesAlias_NullRawPayee_ThrowsArgumentException()
    {
        var payee = new Payee("Amazon", _ownerId);

        var act = () => payee.MatchesAlias(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MatchesAlias_EmptyRawPayee_ThrowsArgumentException()
    {
        var payee = new Payee("Amazon", _ownerId);

        var act = () => payee.MatchesAlias("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MatchesAlias_WhitespaceRawPayee_ThrowsArgumentException()
    {
        var payee = new Payee("Amazon", _ownerId);

        var act = () => payee.MatchesAlias("   ");

        act.Should().Throw<ArgumentException>();
    }
}
