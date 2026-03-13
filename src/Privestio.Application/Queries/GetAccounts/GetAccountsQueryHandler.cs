using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Application.Queries.GetAccounts;

public class GetAccountsQueryHandler
    : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var nonPropertyIds = accounts
            .Where(a => a.AccountType != AccountType.Property)
            .Select(a => a.Id);

        var signedSums = await _unitOfWork.Transactions.GetSignedSumsByAccountIdsAsync(
            nonPropertyIds,
            cancellationToken
        );

        return accounts
            .Select(a => AccountMapper.ToResponse(a, ComputeBalance(a, signedSums)))
            .ToList()
            .AsReadOnly();
    }

    private static decimal ComputeBalance(
        Account account,
        IReadOnlyDictionary<Guid, decimal> signedSums
    )
    {
        if (account.AccountType == AccountType.Property)
        {
            var latest = account.GetLatestValuation();
            return latest?.EstimatedValue.Amount ?? account.OpeningBalance.Amount;
        }

        signedSums.TryGetValue(account.Id, out var sum);
        return account.OpeningBalance.Amount + sum;
    }
}
