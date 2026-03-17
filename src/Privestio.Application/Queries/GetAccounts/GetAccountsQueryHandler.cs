using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetAccounts;

public class GetAccountsQueryHandler
    : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;

    public GetAccountsQueryHandler(
        IUnitOfWork unitOfWork,
        InvestmentPortfolioValuationService investmentPortfolioValuationService
    )
    {
        _unitOfWork = unitOfWork;
        _investmentPortfolioValuationService = investmentPortfolioValuationService;
    }

    public async Task<IReadOnlyList<AccountResponse>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(
            request.OwnerId,
            cancellationToken
        );

        var nonPropertyAndNonInvestmentIds = accounts
            .Where(a =>
                a.AccountType != AccountType.Property && a.AccountType != AccountType.Investment
            )
            .Select(a => a.Id);

        var signedSums = await _unitOfWork.Transactions.GetSignedSumsByAccountIdsAsync(
            nonPropertyAndNonInvestmentIds,
            cancellationToken
        );

        var responses = new List<AccountResponse>(accounts.Count);
        foreach (var account in accounts)
        {
            var balance = await ComputeBalanceAsync(account, signedSums, cancellationToken);
            responses.Add(AccountMapper.ToResponse(account, balance));
        }

        return responses.AsReadOnly();
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
