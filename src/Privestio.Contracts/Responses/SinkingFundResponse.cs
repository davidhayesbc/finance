namespace Privestio.Contracts.Responses;

public record SinkingFundResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public decimal AccumulatedAmount { get; init; }
    public decimal MonthlySetAside { get; init; }
    public decimal ProgressPercentage { get; init; }
    public bool IsOnTrack { get; init; }
    public DateTime DueDate { get; init; }
    public string Currency { get; init; } = "CAD";
    public Guid? AccountId { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
