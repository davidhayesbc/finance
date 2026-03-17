namespace Privestio.Contracts.Responses;

public record SecurityAliasResponse
{
    public Guid Id { get; init; }
    public Guid SecurityId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string? Source { get; init; }
    public bool IsPrimary { get; init; }
}
