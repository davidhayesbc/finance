namespace Privestio.Contracts.Requests;

public record AcceptRuleSuggestionRequest
{
    public string Name { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string Conditions { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public bool IsEnabled { get; init; } = true;
    public RuleApplyScope ApplyScope { get; init; } = RuleApplyScope.AllMatching;
}
