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
}
