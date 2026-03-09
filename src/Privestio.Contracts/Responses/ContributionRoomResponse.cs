namespace Privestio.Contracts.Responses;

public record ContributionRoomResponse
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal AnnualLimitAmount { get; init; }
    public decimal CarryForwardAmount { get; init; }
    public decimal ContributionsYtdAmount { get; init; }
    public decimal RemainingRoomAmount { get; init; }
    public string Currency { get; init; } = "CAD";
}
