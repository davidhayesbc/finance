namespace Privestio.Contracts.Responses;

public record SecurityIdentifierResponse
{
    public Guid Id { get; init; }
    public Guid SecurityId { get; init; }
    public string IdentifierType { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}
