namespace Privestio.Contracts.Pagination;

/// <summary>
/// Cursor-based pagination wrapper for list responses.
/// </summary>
public record PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageSize { get; init; }
    public string? NextCursor { get; init; }
    public bool HasNextPage => NextCursor is not null;
}
