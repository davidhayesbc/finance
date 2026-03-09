namespace Privestio.Contracts.Responses;

public record SpendingAnalysisResponse
{
    public decimal TotalSpent { get; init; }
    public string Currency { get; init; } = "CAD";
    public IReadOnlyList<CategoryBreakdownItem> CategoryBreakdown { get; init; } = [];
    public IReadOnlyList<PayeeRankingItem> PayeeRanking { get; init; } = [];
    public IReadOnlyList<MonthlyTrendItem> MonthlyTrends { get; init; } = [];
}

public record CategoryBreakdownItem
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Percentage { get; init; }
}

public record PayeeRankingItem
{
    public string PayeeName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int TransactionCount { get; init; }
}

public record MonthlyTrendItem
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Amount { get; init; }
}
