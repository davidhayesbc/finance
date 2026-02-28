using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a hierarchical transaction category.
/// Categories form a tree with optional parent references for group roll-up reporting.
/// </summary>
public class Category : BaseEntity
{
    private readonly List<Category> _children = [];

    private Category() { }

    public Category(
        string name,
        CategoryType type,
        Guid? ownerId = null,
        Guid? parentCategoryId = null,
        string? icon = null,
        int sortOrder = 0,
        bool isSystem = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Type = type;
        OwnerId = ownerId;
        ParentCategoryId = parentCategoryId;
        Icon = icon;
        SortOrder = sortOrder;
        IsSystem = isSystem;
    }

    public string Name { get; private set; } = string.Empty;
    public CategoryType Type { get; private set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystem { get; private set; }

    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; set; }

    public Guid? OwnerId { get; private set; }
    public User? Owner { get; set; }

    public IReadOnlyCollection<Category> Children => _children.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParent(Guid? parentCategoryId)
    {
        ParentCategoryId = parentCategoryId;
        UpdatedAt = DateTime.UtcNow;
    }
}
