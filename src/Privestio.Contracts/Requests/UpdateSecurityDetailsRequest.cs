namespace Privestio.Contracts.Requests;

public record UpdateSecurityDetailsRequest
{
    public string Name { get; init; } = string.Empty;
    public string DisplaySymbol { get; init; } = string.Empty;
    public string Currency { get; init; } = "CAD";
    public string? Exchange { get; init; }
    public bool IsCashEquivalent { get; init; }
}
