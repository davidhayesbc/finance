using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class ReconciliationPeriodTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesReconciliationPeriod()
    {
        var balance = new Money(1500m, "CAD");
        var statementDate = new DateOnly(2026, 2, 28);

        var period = new ReconciliationPeriod(AccountId, statementDate, balance);

        period.AccountId.Should().Be(AccountId);
        period.StatementDate.Should().Be(statementDate);
        period.StatementBalance.Should().Be(balance);
        period.Status.Should().Be(ReconciliationStatus.Open);
        period.LockedAt.Should().BeNull();
        period.LockedByUserId.Should().BeNull();
        period.UnlockReason.Should().BeNull();
        period.Notes.Should().BeNull();
        period.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithNotes_SetsNotes()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m),
            "February statement"
        );

        period.Notes.Should().Be("February statement");
    }

    [Fact]
    public void MarkBalanced_SetsStatusToBalanced()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m)
        );

        period.MarkBalanced();

        period.Status.Should().Be(ReconciliationStatus.Balanced);
    }

    [Fact]
    public void Lock_FromBalanced_SetsStatusToLocked()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m)
        );
        period.MarkBalanced();
        var userId = Guid.NewGuid();

        period.Lock(userId);

        period.Status.Should().Be(ReconciliationStatus.Locked);
        period.LockedByUserId.Should().Be(userId);
        period.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public void Lock_FromOpen_ThrowsInvalidOperationException()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m)
        );

        var act = () => period.Lock(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("*balanced*");
    }

    [Fact]
    public void Unlock_WithReason_SetsStatusToOpen()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m)
        );
        period.MarkBalanced();
        period.Lock(Guid.NewGuid());

        period.Unlock("Correction needed");

        period.Status.Should().Be(ReconciliationStatus.Open);
        period.UnlockReason.Should().Be("Correction needed");
        period.LockedAt.Should().BeNull();
        period.LockedByUserId.Should().BeNull();
    }

    [Fact]
    public void Unlock_WithoutReason_ThrowsArgumentException()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m)
        );
        period.MarkBalanced();
        period.Lock(Guid.NewGuid());

        var act = () => period.Unlock("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Unlock_WhenNotLocked_ThrowsInvalidOperationException()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m)
        );

        var act = () => period.Unlock("Reason");

        act.Should().Throw<InvalidOperationException>().WithMessage("*locked*");
    }

    [Fact]
    public void UpdateNotes_SetsNotes()
    {
        var period = new ReconciliationPeriod(
            AccountId,
            new DateOnly(2026, 2, 28),
            new Money(1000m)
        );

        period.UpdateNotes("Updated note");

        period.Notes.Should().Be("Updated note");
    }
}
