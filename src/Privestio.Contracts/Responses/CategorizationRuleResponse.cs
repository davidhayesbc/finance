namespace Privestio.Contracts.Responses;

public record CategorizationRuleResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string Conditions { get; init; } = string.Empty;
    public string Actions { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
