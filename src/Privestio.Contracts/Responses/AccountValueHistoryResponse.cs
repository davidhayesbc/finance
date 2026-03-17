namespace Privestio.Contracts.Responses;

public record AccountValueHistoryResponse
{
    public Guid AccountId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public IReadOnlyList<ValueHistoryPointResponse> Points { get; init; } = [];
}
