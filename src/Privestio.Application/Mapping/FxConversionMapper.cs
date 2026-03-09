using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class FxConversionMapper
{
    public static FxConversionResponse ToResponse(FxConversion conversion) =>
        new()
        {
            Id = conversion.Id,
            TransactionId = conversion.TransactionId,
            OriginalAmount = conversion.OriginalAmount.Amount,
            OriginalCurrency = conversion.OriginalAmount.CurrencyCode,
            ConvertedAmount = conversion.ConvertedAmount.Amount,
            ConvertedCurrency = conversion.ConvertedAmount.CurrencyCode,
            AppliedRate = conversion.AppliedRate,
            ExchangeRateId = conversion.ExchangeRateId,
        };
}
