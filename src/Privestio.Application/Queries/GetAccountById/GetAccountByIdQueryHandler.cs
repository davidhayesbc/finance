using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetAccountById;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InvestmentPortfolioValuationService _investmentPortfolioValuationService;

    public GetAccountByIdQueryHandler(
        IUnitOfWork unitOfWork,
        InvestmentPortfolioValuationService investmentPortfolioValuationService
    )
    {
        _unitOfWork = unitOfWork;
        _investmentPortfolioValuationService = investmentPortfolioValuationService;
    }

    public async Task<AccountResponse?> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetAccessibleByIdAsync(
            request.AccountId,
            request.RequestingUserId,
            cancellationToken
        );

        if (account is null)
            return null;

        var balance = await ComputeCurrentBalanceAsync(account, cancellationToken);
        return AccountMapper.ToResponse(account, balance);
    }

    private async Task<decimal> ComputeCurrentBalanceAsync(
        Account account,
        CancellationToken cancellationToken
    )
    {
        if (account.AccountType == AccountType.Investment)
        {
            var valuation = await _investmentPortfolioValuationService.CalculateAsync(
                account,
                cancellationToken
            );
            return valuation.TotalMarketValue ?? account.CurrentBalance.Amount;
        }

        if (account.AccountType == AccountType.Property)
        {
            var latest = account.GetLatestValuation();
            return latest?.EstimatedValue.Amount ?? account.OpeningBalance.Amount;
        }

        var signedSum = await _unitOfWork.Transactions.GetSignedSumByAccountIdAsync(
            account.Id,
            cancellationToken
        );
        return account.OpeningBalance.Amount + signedSum;
    }
}
