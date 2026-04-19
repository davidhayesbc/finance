using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetCashFlowSummary;

public class GetCashFlowSummaryQueryHandler
    : IRequestHandler<GetCashFlowSummaryQuery, CashFlowSummaryResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCashFlowSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CashFlowSummaryResponse> Handle(
        GetCashFlowSummaryQuery request,
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

        var nonTransferTransactions = transactions
            .Where(t => t.Type != TransactionType.Transfer)
            .ToList();

        var totalIncome = nonTransferTransactions
            .Where(t => t.Type == TransactionType.Credit)
            .Sum(t => Math.Abs(t.Amount.Amount));

        var totalExpenses = nonTransferTransactions
            .Where(t => t.Type == TransactionType.Debit)
            .Sum(t => Math.Abs(t.Amount.Amount));

        var netSavings = totalIncome - totalExpenses;
        var savingsRate = totalIncome > 0 ? Math.Round(netSavings / totalIncome * 100, 2) : 0;

        var monthlyBreakdown = nonTransferTransactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthlyBreakdownItem
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Income = g.Where(t => t.Type == TransactionType.Credit)
                    .Sum(t => Math.Abs(t.Amount.Amount)),
                Expenses = g.Where(t => t.Type == TransactionType.Debit)
                    .Sum(t => Math.Abs(t.Amount.Amount)),
                Net =
                    g.Where(t => t.Type == TransactionType.Credit)
                        .Sum(t => Math.Abs(t.Amount.Amount))
                    - g.Where(t => t.Type == TransactionType.Debit)
                        .Sum(t => Math.Abs(t.Amount.Amount)),
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        var baseCurrency = nonTransferTransactions
            .Select(t => t.Amount.CurrencyCode)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "CAD";

        return new CashFlowSummaryResponse
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetSavings = netSavings,
            SavingsRate = savingsRate,
            Currency = baseCurrency,
            MonthlyBreakdown = monthlyBreakdown,
        };
    }
}
