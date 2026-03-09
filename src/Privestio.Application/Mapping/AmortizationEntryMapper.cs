using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class AmortizationEntryMapper
{
    public static AmortizationEntryResponse ToResponse(AmortizationEntry entry) =>
        new()
        {
            Id = entry.Id,
            PaymentNumber = entry.PaymentNumber,
            PaymentDate = entry.PaymentDate,
            PaymentAmount = entry.PaymentAmount.Amount,
            PrincipalAmount = entry.PrincipalAmount.Amount,
            InterestAmount = entry.InterestAmount.Amount,
            RemainingBalance = entry.RemainingBalance.Amount,
        };
}
