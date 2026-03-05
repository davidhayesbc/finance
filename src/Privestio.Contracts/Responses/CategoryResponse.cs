namespace Privestio.Contracts.Responses;

public record CategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsSystem { get; init; }
    public Guid? ParentCategoryId { get; init; }
}
