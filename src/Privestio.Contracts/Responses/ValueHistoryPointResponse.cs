namespace Privestio.Contracts.Responses;

public record ValueHistoryPointResponse
{
    public DateOnly Date { get; init; }
    public decimal Value { get; init; }
}
