namespace Privestio.Contracts.Requests;

public record CreateCategoryRequest(
    string Name,
    string Type,
    string? Icon = null,
    int SortOrder = 0,
    Guid? ParentCategoryId = null
);
