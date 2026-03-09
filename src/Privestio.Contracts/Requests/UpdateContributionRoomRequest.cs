namespace Privestio.Contracts.Requests;

public record UpdateContributionRoomRequest(
    decimal AnnualLimitAmount,
    decimal CarryForwardAmount,
    decimal? ContributionAmount,
    string Currency = "CAD"
);
