namespace Privestio.Contracts.Responses;

public record AmortizationEntryResponse
{
    public Guid Id { get; init; }
    public int PaymentNumber { get; init; }
    public DateOnly PaymentDate { get; init; }
    public decimal PaymentAmount { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestAmount { get; init; }
    public decimal RemainingBalance { get; init; }
}
