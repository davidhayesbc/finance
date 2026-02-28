using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class TransactionTests
{
    private static Guid _accountId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesTransaction()
    {
        var amount = new Money(100.00m, "CAD");
        var txn = new Transaction(_accountId, DateTime.UtcNow, amount, "Test transaction", TransactionType.Debit);

        txn.AccountId.Should().Be(_accountId);
        txn.Amount.Should().Be(amount);
        txn.Description.Should().Be("Test transaction");
        txn.Type.Should().Be(TransactionType.Debit);
        txn.IsSplit.Should().BeFalse();
    }

    [Fact]
    public void Constructor_EmptyDescription_ThrowsArgumentException()
    {
        var amount = new Money(100.00m, "CAD");

        var act = () => new Transaction(_accountId, DateTime.UtcNow, amount, "", TransactionType.Debit);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddSplit_AddsSplitToTransaction()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);
        var split = new TransactionSplit(txn.Id, new Money(100.00m), Guid.NewGuid());

        txn.AddSplit(split);

        txn.IsSplit.Should().BeTrue();
        txn.Splits.Should().HaveCount(1);
    }

    [Fact]
    public void AddSplit_NullSplit_ThrowsArgumentNullException()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);

        var act = () => txn.AddSplit(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateSplitInvariant_SplitsSumToParent_ReturnsTrue()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);
        var split1 = new TransactionSplit(txn.Id, new Money(60.00m), Guid.NewGuid());
        var split2 = new TransactionSplit(txn.Id, new Money(40.00m), Guid.NewGuid());

        txn.AddSplit(split1);
        txn.AddSplit(split2);

        txn.ValidateSplitInvariant().Should().BeTrue();
    }

    [Fact]
    public void ValidateSplitInvariant_SplitsDoNotSumToParent_ReturnsFalse()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);
        var split1 = new TransactionSplit(txn.Id, new Money(60.00m), Guid.NewGuid());
        var split2 = new TransactionSplit(txn.Id, new Money(30.00m), Guid.NewGuid());

        txn.AddSplit(split1);
        txn.AddSplit(split2);

        txn.ValidateSplitInvariant().Should().BeFalse();
    }

    [Fact]
    public void ValidateSplitInvariant_NoSplits_ReturnsTrue()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);

        txn.ValidateSplitInvariant().Should().BeTrue();
    }

    [Fact]
    public void ClearSplits_RemovesAllSplits()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(100.00m), Guid.NewGuid()));

        txn.ClearSplits();

        txn.Splits.Should().BeEmpty();
        txn.IsSplit.Should().BeFalse();
    }

    [Fact]
    public void AddTag_AddsTagToTransaction()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);
        var tag = new Tag("groceries", Guid.NewGuid());

        txn.AddTag(tag);

        txn.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void AddTag_DuplicateTag_DoesNotAddTwice()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);
        var tag = new Tag("groceries", Guid.NewGuid());

        txn.AddTag(tag);
        txn.AddTag(tag);

        txn.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveTag_ExistingTag_RemovesTag()
    {
        var txn = new Transaction(_accountId, DateTime.UtcNow, new Money(100.00m), "Test", TransactionType.Debit);
        var tag = new Tag("groceries", Guid.NewGuid());
        txn.AddTag(tag);

        txn.RemoveTag(tag.Id);

        txn.Tags.Should().BeEmpty();
    }
}
