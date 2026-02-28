namespace Privestio.Contracts.Responses;

public record ErrorResponse
{
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string? Detail { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
}
