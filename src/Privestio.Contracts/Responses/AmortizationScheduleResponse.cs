namespace Privestio.Contracts.Responses;

public record AmortizationScheduleResponse
{
    public Guid AccountId { get; init; }
    public IReadOnlyList<AmortizationEntryResponse> Entries { get; init; } = [];
    public decimal TotalInterest { get; init; }
    public decimal TotalPrincipal { get; init; }
    public string Currency { get; init; } = "CAD";
}
