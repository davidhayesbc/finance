using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class ValuationMapper
{
    public static ValuationResponse ToResponse(Valuation valuation) =>
        new()
        {
            Id = valuation.Id,
            AccountId = valuation.AccountId,
            Amount = valuation.EstimatedValue.Amount,
            Currency = valuation.EstimatedValue.CurrencyCode,
            EffectiveDate = valuation.EffectiveDate,
            RecordedAt = valuation.RecordedAt,
            Source = valuation.Source,
            Notes = valuation.Notes,
        };
}
