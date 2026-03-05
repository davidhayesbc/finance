namespace Privestio.Contracts.Responses;

public record ImportResultResponse
{
    public Guid ImportBatchId { get; init; }
    public int TotalRows { get; init; }
    public int ImportedCount { get; init; }
    public int DuplicateCount { get; init; }
    public int ErrorCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<ImportErrorDetail> Errors { get; init; } = [];
}

public record ImportErrorDetail
{
    public int RowNumber { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? RawData { get; init; }
}
