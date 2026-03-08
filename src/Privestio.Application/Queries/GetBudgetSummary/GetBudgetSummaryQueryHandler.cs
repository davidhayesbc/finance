using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetBudgetSummary;

public class GetBudgetSummaryQueryHandler
    : IRequestHandler<GetBudgetSummaryQuery, IReadOnlyList<BudgetSummaryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBudgetSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BudgetSummaryResponse>> Handle(
        GetBudgetSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var budgets = await _unitOfWork.Budgets.GetByUserIdAndPeriodAsync(
            request.UserId,
            request.Year,
            request.Month,
            cancellationToken
        );

        if (budgets.Count == 0)
            return Array.Empty<BudgetSummaryResponse>();

        // Get all transactions for the period (split-aware)
        var startDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var transactions = await _unitOfWork.Transactions.GetByOwnerAndDateRangeAsync(
            request.UserId,
            startDate,
            endDate,
            cancellationToken
        );

        // Calculate actual spending per category, split-aware:
        // If a transaction has splits, use split categories; otherwise use transaction's category
        var actualByCategory = new Dictionary<Guid, decimal>();

        foreach (var txn in transactions)
        {
            if (txn.Type == TransactionType.Transfer)
                continue;

            if (txn.IsSplit)
            {
                foreach (var split in txn.Splits.Where(s => !s.IsDeleted))
                {
                    if (!actualByCategory.TryGetValue(split.CategoryId, out var current))
                        current = 0m;
                    actualByCategory[split.CategoryId] = current + Math.Abs(split.Amount.Amount);
                }
            }
            else if (txn.CategoryId.HasValue)
            {
                if (!actualByCategory.TryGetValue(txn.CategoryId.Value, out var current))
                    current = 0m;
                actualByCategory[txn.CategoryId.Value] = current + Math.Abs(txn.Amount.Amount);
            }
        }

        var summaries = budgets
            .Select(budget =>
            {
                actualByCategory.TryGetValue(budget.CategoryId, out var actual);
                var remaining = budget.Amount.Amount - actual;
                var percentUsed =
                    budget.Amount.Amount > 0
                        ? Math.Round(
                            actual / budget.Amount.Amount * 100,
                            2,
                            MidpointRounding.ToEven
                        )
                        : 0m;

                return new BudgetSummaryResponse
                {
                    CategoryId = budget.CategoryId,
                    CategoryName = budget.Category?.Name ?? string.Empty,
                    Year = budget.Year,
                    Month = budget.Month,
                    BudgetedAmount = budget.Amount.Amount,
                    ActualAmount = actual,
                    RemainingAmount = remaining,
                    PercentageUsed = percentUsed,
                    Currency = budget.Amount.CurrencyCode,
                    IsOverBudget = actual > budget.Amount.Amount,
                };
            })
            .ToList();

        return summaries.AsReadOnly();
    }
}
