using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_WithDates_CreatesInstance()
    {
        var start = new DateOnly(2024, 1, 1);
        var end = new DateOnly(2024, 12, 31);

        var range = new DateRange(start, end);

        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void Contains_DateWithinRange_ReturnsTrue()
    {
        var range = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        range.Contains(new DateOnly(2024, 6, 15)).Should().BeTrue();
    }

    [Fact]
    public void Contains_DateBeforeRange_ReturnsFalse()
    {
        var range = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        range.Contains(new DateOnly(2023, 12, 31)).Should().BeFalse();
    }

    [Fact]
    public void Contains_DateAfterRange_ReturnsFalse()
    {
        var range = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        range.Contains(new DateOnly(2025, 1, 1)).Should().BeFalse();
    }

    [Fact]
    public void Contains_StartDate_ReturnsTrue()
    {
        var start = new DateOnly(2024, 1, 1);
        var range = new DateRange(start, new DateOnly(2024, 12, 31));

        range.Contains(start).Should().BeTrue();
    }

    [Fact]
    public void Contains_EndDate_ReturnsTrue()
    {
        var end = new DateOnly(2024, 12, 31);
        var range = new DateRange(new DateOnly(2024, 1, 1), end);

        range.Contains(end).Should().BeTrue();
    }

    [Fact]
    public void ForMonth_CreatesCorrectRange()
    {
        var range = DateRange.ForMonth(2024, 2);

        range.Start.Should().Be(new DateOnly(2024, 2, 1));
        range.End.Should().Be(new DateOnly(2024, 2, 29)); // 2024 is a leap year
    }

    [Fact]
    public void ForYear_CreatesCorrectRange()
    {
        var range = DateRange.ForYear(2024);

        range.Start.Should().Be(new DateOnly(2024, 1, 1));
        range.End.Should().Be(new DateOnly(2024, 12, 31));
    }

    [Fact]
    public void DayCount_ReturnsCorrectCount()
    {
        var range = DateRange.ForMonth(2024, 1);

        range.DayCount.Should().Be(31);
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ReturnsTrue()
    {
        var a = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30));
        var b = new DateRange(new DateOnly(2024, 4, 1), new DateOnly(2024, 12, 31));

        a.Overlaps(b).Should().BeTrue();
        b.Overlaps(a).Should().BeTrue();
    }

    [Fact]
    public void Overlaps_NonOverlappingRanges_ReturnsFalse()
    {
        var a = new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31));
        var b = new DateRange(new DateOnly(2024, 4, 1), new DateOnly(2024, 12, 31));

        a.Overlaps(b).Should().BeFalse();
    }
}
