namespace Privestio.Contracts.Responses;

public record RuleSuggestionResponse
{
    public string Name { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string Conditions { get; init; } = string.Empty;
    public string SuggestedCategoryName { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
    public int MatchCount { get; init; }
    public decimal MatchRate { get; init; }
    public IReadOnlyList<RuleSuggestionMatchSampleResponse> MatchSamples { get; init; } = [];
}

public record RuleSuggestionMatchSampleResponse
{
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Frequency { get; init; }
}
