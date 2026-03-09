namespace Privestio.Contracts.Responses;

public record NetWorthSummaryResponse
{
    public decimal TotalAssets { get; init; }
    public decimal TotalLiabilities { get; init; }
    public decimal NetWorth { get; init; }
    public string Currency { get; init; } = "CAD";
    public IReadOnlyList<AssetAllocationItem> AssetAllocation { get; init; } = [];
    public IReadOnlyList<AccountSummary> AccountSummaries { get; init; } = [];
}

public record AssetAllocationItem
{
    public string AccountType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Percentage { get; init; }
}

public record AccountSummary
{
    public Guid AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "CAD";
}
