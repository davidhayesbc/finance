namespace Privestio.Contracts.Requests;

public record AddSecurityAliasRequest
{
    public string Symbol { get; init; } = string.Empty;
    public string? Source { get; init; }
    public string? Exchange { get; init; }
    public bool IsPrimary { get; init; }
}
