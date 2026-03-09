using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class ExchangeRateMapper
{
    public static ExchangeRateResponse ToResponse(ExchangeRate rate) =>
        new()
        {
            Id = rate.Id,
            FromCurrency = rate.FromCurrency,
            ToCurrency = rate.ToCurrency,
            Rate = rate.Rate,
            AsOfDate = rate.AsOfDate,
            RecordedAt = rate.RecordedAt,
            Source = rate.Source,
        };
}
