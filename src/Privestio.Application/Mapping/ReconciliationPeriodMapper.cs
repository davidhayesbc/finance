using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class ReconciliationPeriodMapper
{
    public static ReconciliationPeriodResponse ToResponse(ReconciliationPeriod period) =>
        new()
        {
            Id = period.Id,
            AccountId = period.AccountId,
            StatementDate = period.StatementDate,
            StatementBalanceAmount = period.StatementBalance.Amount,
            Currency = period.StatementBalance.CurrencyCode,
            Status = period.Status.ToString(),
            LockedAt = period.LockedAt,
            LockedByUserId = period.LockedByUserId,
            UnlockReason = period.UnlockReason,
            Notes = period.Notes,
            CreatedAt = period.CreatedAt,
            UpdatedAt = period.UpdatedAt,
        };
}
