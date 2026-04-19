using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetSpendingAnalysis;

public class GetSpendingAnalysisQueryHandler
    : IRequestHandler<GetSpendingAnalysisQuery, SpendingAnalysisResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSpendingAnalysisQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpendingAnalysisResponse> Handle(
        GetSpendingAnalysisQuery request,
        CancellationToken cancellationToken
    )
    {
        var startDate = request.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDate = request.EndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var transactions = await _unitOfWork.Transactions.GetByOwnerAndDateRangeAsync(
            request.UserId,
            startDate,
            endDate,
            cancellationToken
        );

        var debitTransactions = transactions.Where(t => t.Type == TransactionType.Debit).ToList();

        // Split-aware category breakdown
        var categoryAmounts = new Dictionary<Guid, (string Name, decimal Amount)>();

        foreach (var txn in debitTransactions)
        {
            if (txn.IsSplit)
            {
                foreach (var split in txn.Splits.Where(s => !s.IsDeleted))
                {
                    var categoryName = split.Category?.Name ?? "Uncategorized";
                    if (categoryAmounts.TryGetValue(split.CategoryId, out var existing))
                    {
                        categoryAmounts[split.CategoryId] = (
                            categoryName,
                            existing.Amount + Math.Abs(split.Amount.Amount)
                        );
                    }
                    else
                    {
                        categoryAmounts[split.CategoryId] = (
                            categoryName,
                            Math.Abs(split.Amount.Amount)
                        );
                    }
                }
            }
            else if (txn.CategoryId.HasValue)
            {
                var categoryName = txn.Category?.Name ?? "Uncategorized";
                if (categoryAmounts.TryGetValue(txn.CategoryId.Value, out var existing))
                {
                    categoryAmounts[txn.CategoryId.Value] = (
                        categoryName,
                        existing.Amount + Math.Abs(txn.Amount.Amount)
                    );
                }
                else
                {
                    categoryAmounts[txn.CategoryId.Value] = (
                        categoryName,
                        Math.Abs(txn.Amount.Amount)
                    );
                }
            }
        }

        var totalSpent = debitTransactions.Sum(t => Math.Abs(t.Amount.Amount));

        var categoryBreakdown = categoryAmounts
            .Select(kvp => new CategoryBreakdownItem
            {
                CategoryId = kvp.Key,
                CategoryName = kvp.Value.Name,
                Amount = kvp.Value.Amount,
                Percentage =
                    totalSpent > 0 ? Math.Round(kvp.Value.Amount / totalSpent * 100, 2) : 0,
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Payee ranking
        var payeeRanking = debitTransactions
            .Where(t => t.Payee is not null)
            .GroupBy(t => t.Payee!.DisplayName)
            .Select(g => new PayeeRankingItem
            {
                PayeeName = g.Key,
                Amount = g.Sum(t => Math.Abs(t.Amount.Amount)),
                TransactionCount = g.Count(),
            })
            .OrderByDescending(p => p.Amount)
            .ToList();

        // Monthly trends
        var monthlyTrends = debitTransactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthlyTrendItem
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Amount = g.Sum(t => Math.Abs(t.Amount.Amount)),
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        var baseCurrency = debitTransactions
            .Select(t => t.Amount.CurrencyCode)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "CAD";

        return new SpendingAnalysisResponse
        {
            TotalSpent = totalSpent,
            Currency = baseCurrency,
            CategoryBreakdown = categoryBreakdown,
            PayeeRanking = payeeRanking,
            MonthlyTrends = monthlyTrends,
        };
    }
}
