namespace Privestio.Contracts.Responses;

public record AcceptRuleSuggestionResponse
{
    public CategorizationRuleResponse Rule { get; init; } = new();
    public int AppliedTransactionCount { get; init; }
}
