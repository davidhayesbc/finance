using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetNetWorthSummary;

public class GetNetWorthSummaryQueryHandler
    : IRequestHandler<GetNetWorthSummaryQuery, NetWorthSummaryResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNetWorthSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var totalAssets = activeAccounts
            .Where(a => assetTypes.Contains(a.AccountType))
            .Sum(a => a.CurrentBalance.Amount);

        var totalLiabilities = activeAccounts
            .Where(a => liabilityTypes.Contains(a.AccountType))
            .Sum(a => Math.Abs(a.CurrentBalance.Amount));

        var netWorth = totalAssets - totalLiabilities;

        var assetAllocation = activeAccounts
            .Where(a => assetTypes.Contains(a.AccountType))
            .GroupBy(a => a.AccountType)
            .Select(g => new AssetAllocationItem
            {
                AccountType = g.Key.ToString(),
                Amount = g.Sum(a => a.CurrentBalance.Amount),
                Percentage =
                    totalAssets > 0
                        ? Math.Round(g.Sum(a => a.CurrentBalance.Amount) / totalAssets * 100, 2)
                        : 0,
            })
            .OrderByDescending(a => a.Amount)
            .ToList();

        var accountSummaries = activeAccounts
            .Select(a => new AccountSummary
            {
                AccountId = a.Id,
                Name = a.Name,
                AccountType = a.AccountType.ToString(),
                Balance = a.CurrentBalance.Amount,
                Currency = a.Currency,
            })
            .ToList();

        return new NetWorthSummaryResponse
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            NetWorth = netWorth,
            Currency = "CAD",
            AssetAllocation = assetAllocation,
            AccountSummaries = accountSummaries,
        };
    }
}
