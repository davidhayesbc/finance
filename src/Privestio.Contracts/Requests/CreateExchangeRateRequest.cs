namespace Privestio.Contracts.Requests;

public record CreateExchangeRateRequest(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateOnly AsOfDate,
    string Source
);
