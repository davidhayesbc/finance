namespace Privestio.Contracts.Requests;

public record AddSecurityIdentifierRequest
{
    public string IdentifierType { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}
