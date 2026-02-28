namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a managed tag entity for labelling transactions and splits.
/// </summary>
public class Tag : BaseEntity
{
    private Tag() { }

    public Tag(string name, Guid ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        OwnerId = ownerId;
    }

    public string Name { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public User? Owner { get; set; }

    public ICollection<TransactionTag> TransactionTags { get; } = new List<TransactionTag>();
    public ICollection<TransactionSplitTag> TransactionSplitTags { get; } = new List<TransactionSplitTag>();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
