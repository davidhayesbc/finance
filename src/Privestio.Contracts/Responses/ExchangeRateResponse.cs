namespace Privestio.Contracts.Responses;

public record ExchangeRateResponse
{
    public Guid Id { get; init; }
    public string FromCurrency { get; init; } = string.Empty;
    public string ToCurrency { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateOnly AsOfDate { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = string.Empty;
}
