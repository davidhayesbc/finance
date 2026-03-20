namespace Privestio.Contracts.Responses;

public record NetWorthHistoryResponse
{
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<ValueHistoryPointResponse> Points { get; init; } = [];
    public IReadOnlyList<AccountNetWorthSeries> Series { get; init; } = [];
}

public record AccountNetWorthSeries
{
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public IReadOnlyList<ValueHistoryPointResponse> Points { get; init; } = [];
}
