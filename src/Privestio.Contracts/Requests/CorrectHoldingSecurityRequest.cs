namespace Privestio.Contracts.Requests;

public record CorrectHoldingSecurityRequest
{
    public string Symbol { get; init; } = string.Empty;
    public string? SecurityName { get; init; }
    public string? Source { get; init; }
    public string? Exchange { get; init; }
    public string? Cusip { get; init; }
    public string? Isin { get; init; }
}
