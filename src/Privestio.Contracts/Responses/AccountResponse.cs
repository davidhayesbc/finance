namespace Privestio.Contracts.Responses;

public record AccountResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string AccountSubType { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string? Institution { get; init; }
    public decimal OpeningBalance { get; init; }
    public decimal CurrentBalance { get; init; }
    public DateTime OpeningDate { get; init; }
    public bool IsActive { get; init; }
    public bool IsShared { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
