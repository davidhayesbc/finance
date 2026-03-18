namespace Privestio.Contracts.Responses;

public record ImportHoldingsResultResponse
{
    public Guid ImportBatchId { get; init; }
    public DateOnly StatementDate { get; init; }
    public int TotalHoldings { get; init; }
    public int CreatedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int RemovedCount { get; init; }
    public int ErrorCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<ImportErrorDetail> Errors { get; init; } = [];
}
