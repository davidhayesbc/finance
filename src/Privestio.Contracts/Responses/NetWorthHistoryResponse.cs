namespace Privestio.Contracts.Responses;

public record NetWorthHistoryResponse
{
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<ValueHistoryPointResponse> Points { get; init; } = [];
}
