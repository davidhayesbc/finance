using MediatR;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Commands.CreateExchangeRate;

public record CreateExchangeRateCommand(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly AsOfDate,
    string Source,
    Guid UserId
) : IRequest<ExchangeRateResponse>;
