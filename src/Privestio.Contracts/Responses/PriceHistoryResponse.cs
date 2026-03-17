namespace Privestio.Contracts.Responses;

public record PriceHistoryResponse
{
    public Guid Id { get; init; }
    public Guid SecurityId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string ProviderSymbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateOnly AsOfDate { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = string.Empty;
}
