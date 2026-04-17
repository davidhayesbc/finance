using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Application.Services;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetAccountById;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountResponse?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AccountBalanceService _accountBalanceService;

    public GetAccountByIdQueryHandler(
        IUnitOfWork unitOfWork,
        AccountBalanceService accountBalanceService
    )
    {
        _unitOfWork = unitOfWork;
        _accountBalanceService = accountBalanceService;
    }

    public async Task<AccountResponse?> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null || account.OwnerId != request.RequestingUserId)
            return null;

        var balance = await _accountBalanceService.ComputeCurrentBalanceAsync(
            account,
            cancellationToken
        );
        return AccountMapper.ToResponse(account, balance);
    }
}
