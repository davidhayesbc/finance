namespace Privestio.Contracts.Responses;

public record ValuationResponse
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateOnly EffectiveDate { get; init; }
    public DateTime RecordedAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
