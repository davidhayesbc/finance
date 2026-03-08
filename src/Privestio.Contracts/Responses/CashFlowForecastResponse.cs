namespace Privestio.Contracts.Responses;

public record CashFlowForecastResponse
{
    public IReadOnlyList<CashFlowPeriod> Periods { get; init; } = [];
    public string Currency { get; init; } = "CAD";
}

public record CashFlowPeriod
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal ProjectedIncome { get; init; }
    public decimal ProjectedExpenses { get; init; }
    public decimal ProjectedNet { get; init; }
    public decimal ProjectedBalance { get; init; }
    public decimal BudgetedExpenses { get; init; }
    public decimal SinkingFundContributions { get; init; }
}
