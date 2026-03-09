namespace Privestio.Contracts.Responses;

public record CashFlowSummaryResponse
{
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetSavings { get; init; }
    public decimal SavingsRate { get; init; }
    public string Currency { get; init; } = "CAD";
    public IReadOnlyList<MonthlyBreakdownItem> MonthlyBreakdown { get; init; } = [];
}

public record MonthlyBreakdownItem
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Income { get; init; }
    public decimal Expenses { get; init; }
    public decimal Net { get; init; }
}
