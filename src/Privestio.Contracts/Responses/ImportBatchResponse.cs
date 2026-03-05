namespace Privestio.Contracts.Responses;

public record ImportBatchResponse
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileFormat { get; init; } = string.Empty;
    public DateTime ImportDate { get; init; }
    public int RowCount { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public int DuplicateCount { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal SuccessRate { get; init; }
    public decimal DuplicateRate { get; init; }
    public IReadOnlyList<ImportErrorDetail> Errors { get; init; } = [];
}
