namespace Privestio.Contracts.Requests;

public record UpdateAccountRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Institution { get; init; }
    public string? Notes { get; init; }
    public bool IsShared { get; init; }
}
