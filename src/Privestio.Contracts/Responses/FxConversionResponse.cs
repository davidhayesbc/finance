namespace Privestio.Contracts.Responses;

public record FxConversionResponse
{
    public Guid Id { get; init; }
    public Guid TransactionId { get; init; }
    public decimal OriginalAmount { get; init; }
    public string OriginalCurrency { get; init; } = string.Empty;
    public decimal ConvertedAmount { get; init; }
    public string ConvertedCurrency { get; init; } = string.Empty;
    public decimal AppliedRate { get; init; }
    public Guid ExchangeRateId { get; init; }
}
