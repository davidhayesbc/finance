using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class LotMapper
{
    public static LotResponse ToResponse(Lot lot) =>
        new()
        {
            Id = lot.Id,
            HoldingId = lot.HoldingId,
            AcquiredDate = lot.AcquiredDate,
            Quantity = lot.Quantity,
            UnitCost = lot.UnitCost.Amount,
            Currency = lot.UnitCost.CurrencyCode,
            Source = lot.Source,
            Notes = lot.Notes,
            CreatedAt = lot.CreatedAt,
            UpdatedAt = lot.UpdatedAt,
        };
}
