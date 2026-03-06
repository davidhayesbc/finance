namespace Privestio.Contracts.Requests;

public record UpdateCategorizationRuleRequest
{
    public string Name { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string Conditions { get; init; } = string.Empty;
    public string Actions { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
}
