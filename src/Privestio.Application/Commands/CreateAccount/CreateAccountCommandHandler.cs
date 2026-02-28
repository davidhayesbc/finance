using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateAccount;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAccountCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var accountType = Enum.Parse<Domain.Enums.AccountType>(request.AccountType);
        var accountSubType = Enum.Parse<AccountSubType>(request.AccountSubType);
        var openingBalance = new Money(request.OpeningBalance, request.Currency);

        var account = new Account(
            request.Name,
            accountType,
            accountSubType,
            request.Currency,
            openingBalance,
            request.OpeningDate,
            request.OwnerId,
            request.Institution);

        account.AccountNumber = request.AccountNumber;
        account.Notes = request.Notes;
        account.CurrentBalance = openingBalance;

        await _unitOfWork.Accounts.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
