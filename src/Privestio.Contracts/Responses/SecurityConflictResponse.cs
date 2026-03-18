namespace Privestio.Contracts.Responses;

public record SecurityConflictResponse
{
    public Guid HoldingId { get; init; }
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public string HoldingSymbol { get; init; } = string.Empty;
    public string HoldingSecurityName { get; init; } = string.Empty;
    public IReadOnlyList<SecurityConflictCandidateResponse> Candidates { get; init; } = [];
}

public record SecurityConflictCandidateResponse
{
    public Guid SecurityId { get; init; }
    public string DisplaySymbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string? Exchange { get; init; }
}
