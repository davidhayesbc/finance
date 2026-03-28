namespace Privestio.Contracts.Requests;

public record UpdatePriceHistoryRequest
{
    public decimal Price { get; init; }
    public string Currency { get; init; } = "CAD";
}
