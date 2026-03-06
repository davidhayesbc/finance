namespace Privestio.Contracts.Responses;

public record FilePreviewResponse
{
    public IReadOnlyList<string> DetectedColumns { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> SampleRows { get; init; } = [];
    public int TotalRows { get; init; }
    public string FileFormat { get; init; } = string.Empty;
}
