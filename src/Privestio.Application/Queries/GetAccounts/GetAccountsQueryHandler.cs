using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

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
        return accounts.Select(MapToResponse).ToList().AsReadOnly();
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
