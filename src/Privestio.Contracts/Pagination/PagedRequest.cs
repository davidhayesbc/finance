namespace Privestio.Contracts.Pagination;

/// <summary>
/// Cursor-based pagination request parameters.
/// </summary>
public record PagedRequest
{
    public int PageSize { get; init; } = 20;
    public string? Cursor { get; init; }
}
