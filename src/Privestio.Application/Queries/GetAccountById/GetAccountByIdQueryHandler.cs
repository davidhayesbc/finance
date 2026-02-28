using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

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

        return MapToResponse(account);
    }

    private static AccountResponse MapToResponse(Account account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        AccountType = account.AccountType.ToString(),
        AccountSubType = account.AccountSubType.ToString(),
        Currency = account.Currency,
        Institution = account.Institution,
        OpeningBalance = account.OpeningBalance.Amount,
        CurrentBalance = account.CurrentBalance.Amount,
        OpeningDate = account.OpeningDate,
        IsActive = account.IsActive,
        IsShared = account.IsShared,
        Notes = account.Notes,
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt,
    };
}
