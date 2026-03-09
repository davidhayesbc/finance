namespace Privestio.Contracts.Responses;

public record DebtOverviewResponse
{
    public decimal TotalDebt { get; init; }
    public string Currency { get; init; } = "CAD";
    public IReadOnlyList<DebtDetailItem> Debts { get; init; } = [];
}

public record DebtDetailItem
{
    public Guid AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal AnnualInterestRate { get; init; }
    public decimal MonthlyPayment { get; init; }
    public int RemainingPayments { get; init; }
}
