using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetCashFlowForecast;

public class GetCashFlowForecastQueryHandler
    : IRequestHandler<GetCashFlowForecastQuery, CashFlowForecastResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCashFlowForecastQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashFlowForecastResponse> Handle(
        GetCashFlowForecastQuery request,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var startMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Get active recurring transactions for projections
        var recurrings = await _unitOfWork.RecurringTransactions.GetActiveByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        // Get budgets for the forecast period
        var budgets = await _unitOfWork.Budgets.GetByUserIdAsync(request.UserId, cancellationToken);

        // Get active sinking funds
        var sinkingFunds = await _unitOfWork.SinkingFunds.GetActiveByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        // Get current account balances for starting balance
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            request.UserId,
            cancellationToken
        );

        var currentBalance = accounts
            .Where(a => a.AccountType == AccountType.Banking)
            .Sum(a => a.CurrentBalance.Amount);

        var periods = new List<CashFlowPeriod>();
        var runningBalance = currentBalance;

        for (var i = 0; i < request.Months; i++)
        {
            var periodStart = startMonth.AddMonths(i);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
            var year = periodStart.Year;
            var month = periodStart.Month;

            // Project income and expenses from recurring transactions
            decimal projectedIncome = 0m;
            decimal projectedExpenses = 0m;

            foreach (var recurring in recurrings)
            {
                var occurrences = recurring
                    .ProjectOccurrences(periodEnd)
                    .Count(d => d >= periodStart && d <= periodEnd);

                if (occurrences <= 0)
                    continue;

                var amount = recurring.Amount.Amount * occurrences;

                if (recurring.TransactionType == TransactionType.Credit)
                    projectedIncome += amount;
                else
                    projectedExpenses += amount;
            }

            // Add budgeted expenses for categories without recurring transactions
            var budgetForPeriod = budgets
                .Where(b => b.Year == year && b.Month == month)
                .Sum(b => b.Amount.Amount);

            // Calculate sinking fund contributions for the period
            var sinkingContributions = sinkingFunds.Sum(f =>
                f.CalculateMonthlySetAside(periodStart).Amount
            );

            var projectedNet = projectedIncome - projectedExpenses;
            runningBalance += projectedNet;

            periods.Add(
                new CashFlowPeriod
                {
                    Year = year,
                    Month = month,
                    ProjectedIncome = projectedIncome,
                    ProjectedExpenses = projectedExpenses,
                    ProjectedNet = projectedNet,
                    ProjectedBalance = runningBalance,
                    BudgetedExpenses = budgetForPeriod,
                    SinkingFundContributions = sinkingContributions,
                }
            );
        }

        var baseCurrency = accounts
            .Where(a => a.IsActive)
            .Select(a => a.Currency)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "CAD";

        return new CashFlowForecastResponse { Periods = periods.AsReadOnly(), Currency = baseCurrency };
    }
}
