using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetDebtOverview;

public class GetDebtOverviewQueryHandler
    : IRequestHandler<GetDebtOverviewQuery, DebtOverviewResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDebtOverviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DebtOverviewResponse> Handle(
        GetDebtOverviewQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            request.UserId,
            cancellationToken
        );

        var debtAccounts = accounts
            .Where(a =>
                a.IsActive
                && (a.AccountType == AccountType.Loan || a.AccountType == AccountType.Credit)
            )
            .ToList();

        var debts = new List<DebtDetailItem>();

        foreach (var account in debtAccounts)
        {
            var entries = await _unitOfWork.AmortizationEntries.GetByAccountIdAsync(
                account.Id,
                cancellationToken
            );

            var remainingPayments = entries.Count(e => e.RemainingBalance.Amount > 0);

            var monthlyPayment = entries.FirstOrDefault()?.PaymentAmount.Amount ?? 0m;
            var annualInterestRate = 0m;

            if (entries.Count > 0)
            {
                var firstEntry = entries.First();
                if (firstEntry.PaymentAmount.Amount > 0 && account.CurrentBalance.Amount != 0)
                {
                    annualInterestRate =
                        firstEntry.InterestAmount.Amount
                        / Math.Abs(account.CurrentBalance.Amount)
                        * 12m
                        * 100m;
                    annualInterestRate = Math.Round(annualInterestRate, 2);
                }
            }

            debts.Add(
                new DebtDetailItem
                {
                    AccountId = account.Id,
                    Name = account.Name,
                    Balance = Math.Abs(account.CurrentBalance.Amount),
                    AnnualInterestRate = annualInterestRate,
                    MonthlyPayment = monthlyPayment,
                    RemainingPayments = remainingPayments,
                }
            );
        }

        var baseCurrency = debtAccounts
            .Select(a => a.Currency)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "CAD";

        return new DebtOverviewResponse
        {
            TotalDebt = debts.Sum(d => d.Balance),
            Currency = baseCurrency,
            Debts = debts,
        };
    }
}
