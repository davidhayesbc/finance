using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetNetWorthSummary;

public class GetNetWorthSummaryQueryHandler
    : IRequestHandler<GetNetWorthSummaryQuery, NetWorthSummaryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;

    public GetNetWorthSummaryQueryHandler(
        IUnitOfWork unitOfWork,
        InvestmentPortfolioValuationService investmentPortfolioValuationService
    )
    {
        _unitOfWork = unitOfWork;
        _investmentPortfolioValuationService = investmentPortfolioValuationService;
    }

    public async Task<NetWorthSummaryResponse> Handle(
        GetNetWorthSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            request.UserId,
            cancellationToken
        );

        var activeAccounts = accounts.Where(a => a.IsActive).ToList();

        var assetTypes = new HashSet<AccountType>
        {
            AccountType.Banking,
            AccountType.Investment,
            AccountType.Property,
        };

        var liabilityTypes = new HashSet<AccountType> { AccountType.Credit, AccountType.Loan };

        var nonPropertyAndNonInvestmentIds = activeAccounts
            .Where(a => a.AccountType != AccountType.Property && a.AccountType != AccountType.Investment)
            .Select(a => a.Id);

        var signedSums = await _unitOfWork.Transactions.GetSignedSumsByAccountIdsAsync(
            nonPropertyAndNonInvestmentIds,
            cancellationToken
        );

        var computedAccounts = new List<(Account Account, decimal Balance)>(activeAccounts.Count);
        foreach (var account in activeAccounts)
        {
            var balance = await ComputeBalanceAsync(account, signedSums, cancellationToken);
            computedAccounts.Add((account, balance));
        }

        var totalAssets = computedAccounts
            .Where(x => assetTypes.Contains(x.Account.AccountType))
            .Sum(x => x.Balance);

        var totalLiabilities = computedAccounts
            .Where(x => liabilityTypes.Contains(x.Account.AccountType))
            .Sum(x => Math.Abs(x.Balance));

        var netWorth = totalAssets - totalLiabilities;

        var assetAllocation = computedAccounts
            .Where(x => assetTypes.Contains(x.Account.AccountType))
            .GroupBy(x => x.Account.AccountType)
            .Select(g => new AssetAllocationItem
            {
                AccountType = g.Key.ToString(),
                Amount = g.Sum(x => x.Balance),
                Percentage =
                    totalAssets > 0
                        ? Math.Round(g.Sum(x => x.Balance) / totalAssets * 100, 2)
                        : 0,
            })
            .OrderByDescending(a => a.Amount)
            .ToList();

        var accountSummaries = computedAccounts
            .Select(x => new AccountSummary
            {
                AccountId = x.Account.Id,
                Name = x.Account.Name,
                AccountType = x.Account.AccountType.ToString(),
                Balance = x.Balance,
                Currency = x.Account.Currency,
            })
            .ToList();

        var baseCurrency = activeAccounts
            .Select(a => a.Currency)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "CAD";

        return new NetWorthSummaryResponse
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = netWorth,
            Currency = baseCurrency,
            AssetAllocation = assetAllocation,
            AccountSummaries = accountSummaries,
        };
    }

    private async Task<decimal> ComputeBalanceAsync(
        Account account,
        IReadOnlyDictionary<Guid, decimal> signedSums,
        CancellationToken cancellationToken
    )
    {
        if (account.AccountType == AccountType.Property)
        {
            var latest = account.GetLatestValuation();
            return latest?.EstimatedValue.Amount ?? account.OpeningBalance.Amount;
        }

        if (account.AccountType == AccountType.Investment)
        {
            var valuation = await _investmentPortfolioValuationService.CalculateAsync(
                account,
                cancellationToken
            );
            return valuation.TotalMarketValue ?? account.CurrentBalance.Amount;
        }

        signedSums.TryGetValue(account.Id, out var sum);
        return account.OpeningBalance.Amount + sum;
    }
}
