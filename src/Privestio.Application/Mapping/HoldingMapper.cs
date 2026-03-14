using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class HoldingMapper
{
    public static HoldingResponse ToResponse(Holding holding) =>
        new()
        {
            Id = holding.Id,
            AccountId = holding.AccountId,
            Symbol = holding.Symbol,
            SecurityName = holding.SecurityName,
            Quantity = holding.Quantity,
            AverageCostPerUnit = holding.AverageCostPerUnit.Amount,
            Currency = holding.AverageCostPerUnit.CurrencyCode,
            Notes = holding.Notes,
            CreatedAt = holding.CreatedAt,
            UpdatedAt = holding.UpdatedAt,
        };
}
