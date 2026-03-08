using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.Entities;

public class RecurringTransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArgs_CreatesRecurringTransaction()
    {
        var amount = new Money(100m, "CAD");
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Netflix",
            amount,
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            startDate
        );

        rt.Description.Should().Be("Netflix");
        rt.Amount.Should().Be(amount);
        rt.TransactionType.Should().Be(TransactionType.Debit);
        rt.Frequency.Should().Be(RecurrenceFrequency.Monthly);
        rt.StartDate.Should().Be(startDate);
        rt.EndDate.Should().BeNull();
        rt.NextOccurrence.Should().Be(startDate);
        rt.IsActive.Should().BeTrue();
        rt.LastGenerated.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyDescription_ThrowsArgumentException()
    {
        var act = () =>
            new RecurringTransaction(
                UserId,
                AccountId,
                "",
                new Money(100m),
                TransactionType.Debit,
                RecurrenceFrequency.Monthly,
                DateTime.UtcNow
            );

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var act = () =>
            new RecurringTransaction(
                UserId,
                AccountId,
                "Test",
                new Money(100m),
                TransactionType.Debit,
                RecurrenceFrequency.Monthly,
                start,
                end
            );

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TrimsDescription()
    {
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "  Netflix  ",
            new Money(100m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );

        rt.Description.Should().Be("Netflix");
    }

    [Fact]
    public void AdvanceToNextOccurrence_Monthly_AdvancesByOneMonth()
    {
        var start = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Rent",
            new Money(2000m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            start
        );

        rt.AdvanceToNextOccurrence();

        rt.NextOccurrence.Should().Be(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc));
        rt.LastGenerated.Should().Be(start);
    }

    [Fact]
    public void AdvanceToNextOccurrence_Weekly_AdvancesBySeven()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Groceries",
            new Money(150m),
            TransactionType.Debit,
            RecurrenceFrequency.Weekly,
            start
        );

        rt.AdvanceToNextOccurrence();

        rt.NextOccurrence.Should().Be(new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void AdvanceToNextOccurrence_PastEndDate_DeactivatesRecurrence()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Trial",
            new Money(10m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            start,
            end
        );

        rt.AdvanceToNextOccurrence();

        rt.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ProjectOccurrences_Monthly_ReturnsExpectedDates()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horizon = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Salary",
            new Money(5000m),
            TransactionType.Credit,
            RecurrenceFrequency.Monthly,
            start
        );

        var dates = rt.ProjectOccurrences(horizon);

        dates.Should().HaveCount(4);
        dates[0].Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        dates[1].Should().Be(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        dates[2].Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        dates[3].Should().Be(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ProjectOccurrences_WithEndDate_StopsAtEnd()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var horizon = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Gym",
            new Money(50m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            start,
            end
        );

        var dates = rt.ProjectOccurrences(horizon);

        dates.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(RecurrenceFrequency.BiWeekly, 14)]
    [InlineData(RecurrenceFrequency.Weekly, 7)]
    public void AdvanceToNextOccurrence_DayBasedFrequencies_AdvancesCorrectly(
        RecurrenceFrequency freq,
        int expectedDays
    )
    {
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Test",
            new Money(100m),
            TransactionType.Debit,
            freq,
            start
        );

        rt.AdvanceToNextOccurrence();

        rt.NextOccurrence.Should().Be(start.AddDays(expectedDays));
    }

    [Fact]
    public void Disable_SetsInactive()
    {
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Test",
            new Money(100m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );

        rt.Disable();

        rt.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Enable_SetsActive()
    {
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Test",
            new Money(100m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );
        rt.Disable();

        rt.Enable();

        rt.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateSchedule_EndDateBeforeStart_Throws()
    {
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Test",
            new Money(100m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );

        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var act = () => rt.UpdateSchedule(start, end);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateSchedule_ValidDates_ResetsNextOccurrence()
    {
        var rt = new RecurringTransaction(
            UserId,
            AccountId,
            "Test",
            new Money(100m),
            TransactionType.Debit,
            RecurrenceFrequency.Monthly,
            DateTime.UtcNow
        );
        rt.AdvanceToNextOccurrence();

        var newStart = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        rt.UpdateSchedule(newStart, null);

        rt.NextOccurrence.Should().Be(newStart);
        rt.LastGenerated.Should().BeNull();
    }
}
