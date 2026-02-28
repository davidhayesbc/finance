using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccounts;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AccountResponse>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await _unitOfWork.Accounts.GetByOwnerIdAsync(request.OwnerId, cancellationToken);
        return accounts.Select(AccountMapper.ToResponse).ToList().AsReadOnly();
    }
}
