namespace Privestio.Contracts.Requests;

public record CreateAccountRequest
{
    public string Name { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string AccountSubType { get; init; } = string.Empty;
    public string Currency { get; init; } = "CAD";
    public string? Institution { get; init; }
    public string? AccountNumber { get; init; }
    public decimal OpeningBalance { get; init; }
    public DateOnly OpeningDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string? Notes { get; init; }
}
