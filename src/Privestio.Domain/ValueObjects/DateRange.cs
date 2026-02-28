namespace Privestio.Domain.ValueObjects;

/// <summary>
/// Represents an inclusive date range.
/// </summary>
public readonly record struct DateRange(DateOnly Start, DateOnly End)
{
    public DateRange(DateTime start, DateTime end)
        : this(DateOnly.FromDateTime(start), DateOnly.FromDateTime(end))
    {
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool Contains(DateTime date) => Contains(DateOnly.FromDateTime(date));

    public bool Overlaps(DateRange other) => Start <= other.End && End >= other.Start;

    public int DayCount => End.DayNumber - Start.DayNumber + 1;

    public static DateRange ForMonth(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return new(start, end);
    }

    public static DateRange ForYear(int year) =>
        new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}
