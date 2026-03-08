namespace Privestio.Contracts.Responses;

public record BudgetResponse
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "CAD";
    public bool RolloverEnabled { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
