using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetExchangeRates;

public record GetExchangeRatesQuery(string? FromCurrency, string? ToCurrency)
    : IRequest<IReadOnlyList<ExchangeRateResponse>>;
