using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

/// <summary>
/// Shared mapping methods for converting domain entities to response DTOs.
/// </summary>
public static class AccountMapper
{
    public static AccountResponse ToResponse(Account account) => new()
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
