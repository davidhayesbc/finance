using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetAccounts;

public class GetAccountsQueryHandler
    : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AccountBalanceService _accountBalanceService;

    public GetAccountsQueryHandler(
        IUnitOfWork unitOfWork,
        AccountBalanceService accountBalanceService
    )
    {
        _unitOfWork = unitOfWork;
        _accountBalanceService = accountBalanceService;
    }

    public async Task<IReadOnlyList<AccountResponse>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetAccessibleByUserIdAsync(
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
            var balance = await _accountBalanceService.ComputeCurrentBalanceAsync(
                account,
                signedSums,
                cancellationToken
            );
            responses.Add(AccountMapper.ToResponse(account, balance));
        }

        return responses.AsReadOnly();
    }
}
