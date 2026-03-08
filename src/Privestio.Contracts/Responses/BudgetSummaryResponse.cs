namespace Privestio.Contracts.Responses;

public record BudgetSummaryResponse
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal BudgetedAmount { get; init; }
    public decimal ActualAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal PercentageUsed { get; init; }
    public string Currency { get; init; } = "CAD";
    public bool IsOverBudget { get; init; }
}
