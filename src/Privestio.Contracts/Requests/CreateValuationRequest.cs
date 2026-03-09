namespace Privestio.Contracts.Requests;

public record CreateValuationRequest
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "CAD";
    public DateOnly EffectiveDate { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
