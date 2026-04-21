using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccountUncategorizedCounts;

public class GetAccountUncategorizedCountsQueryHandler
    : IRequestHandler<
        GetAccountUncategorizedCountsQuery,
        IReadOnlyList<AccountUncategorizedCountResponse>
    >
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountUncategorizedCountsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AccountUncategorizedCountResponse>> Handle(
        GetAccountUncategorizedCountsQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts = await _unitOfWork.Accounts.GetAccessibleByUserIdAsync(
            request.UserId,
            cancellationToken
        );
        if (accounts.Count == 0)
        {
            return [];
        }

        var accountIds = accounts.Select(account => account.Id).ToArray();
        var countsByAccountId = await _unitOfWork.Transactions.GetUncategorizedCountsByAccountIdsAsync(
            accountIds,
            cancellationToken
        );

        return accounts
            .Select(account =>
                new AccountUncategorizedCountResponse
                {
                    AccountId = account.Id,
                    UncategorizedCount = countsByAccountId.GetValueOrDefault(account.Id, 0),
                }
            )
            .ToList();
    }
}
