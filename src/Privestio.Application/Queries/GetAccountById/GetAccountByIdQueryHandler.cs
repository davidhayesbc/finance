using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccountById;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAccountByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse?> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null || account.OwnerId != request.RequestingUserId)
            return null;

        return AccountMapper.ToResponse(account);
    }
}
