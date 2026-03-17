using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Mapping;

public static class PriceHistoryMapper
{
    public static PriceHistoryResponse ToResponse(PriceHistory priceHistory) =>
        new()
        {
            Id = priceHistory.Id,
            SecurityId = priceHistory.SecurityId,
            Symbol = priceHistory.Symbol,
            ProviderSymbol = priceHistory.ProviderSymbol,
            Price = priceHistory.Price.Amount,
            Currency = priceHistory.Price.CurrencyCode,
            AsOfDate = priceHistory.AsOfDate,
            RecordedAt = priceHistory.RecordedAt,
            Source = priceHistory.Source,
        };
}
