using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class AccountTests
{
    private static Guid _ownerId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesAccount()
    {
        var opening = new Money(1000.00m, "CAD");
        var account = new Account(
            "My Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            opening,
            DateTime.UtcNow,
            _ownerId,
            "RBC");

        account.Name.Should().Be("My Chequing");
        account.AccountType.Should().Be(AccountType.Banking);
        account.AccountSubType.Should().Be(AccountSubType.Chequing);
        account.Currency.Should().Be("CAD");
        account.OpeningBalance.Should().Be(opening);
        account.Institution.Should().Be("RBC");
        account.OwnerId.Should().Be(_ownerId);
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        var act = () => new Account(
            "",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            Money.Zero(),
            DateTime.UtcNow,
            _ownerId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_CurrencyIsUppercased()
    {
        var account = new Account(
            "Test",
            AccountType.Banking,
            AccountSubType.Chequing,
            "cad",
            Money.Zero(),
            DateTime.UtcNow,
            _ownerId);

        account.Currency.Should().Be("CAD");
    }

    [Fact]
    public void Rename_ValidName_UpdatesName()
    {
        var account = new Account(
            "Old Name",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            Money.Zero(),
            DateTime.UtcNow,
            _ownerId);

        account.Rename("New Name");

        account.Name.Should().Be("New Name");
    }

    [Fact]
    public void Rename_EmptyName_ThrowsArgumentException()
    {
        var account = new Account(
            "Test",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            Money.Zero(),
            DateTime.UtcNow,
            _ownerId);

        var act = () => account.Rename("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var account = new Account(
            "Test",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            Money.Zero(),
            DateTime.UtcNow,
            _ownerId);

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrueAndDeletedAt()
    {
        var account = new Account(
            "Test",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            Money.Zero(),
            DateTime.UtcNow,
            _ownerId);

        account.SoftDelete();

        account.IsDeleted.Should().BeTrue();
        account.DeletedAt.Should().NotBeNull();
    }
}
