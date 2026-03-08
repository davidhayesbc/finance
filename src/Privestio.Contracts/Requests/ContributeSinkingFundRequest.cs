namespace Privestio.Contracts.Requests;

public record ContributeSinkingFundRequest(decimal Amount, string Currency = "CAD");
