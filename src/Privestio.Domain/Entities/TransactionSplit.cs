using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a logical split line of a Transaction.
/// Splits allow a single transaction to be categorized across multiple categories.
/// Invariant: the sum of all split amounts must equal the parent transaction amount.
/// </summary>
public class TransactionSplit : BaseEntity
{
    private readonly List<TransactionSplitTag> _tags = [];

    private TransactionSplit() { }

    public TransactionSplit(
        Guid transactionId,
        Money amount,
        Guid categoryId,
        string? notes = null,
        decimal? percentage = null)
    {
        TransactionId = transactionId;
        Amount = amount;
        CategoryId = categoryId;
        Notes = notes;
        Percentage = percentage;
    }

    public Guid TransactionId { get; private set; }
    public Transaction? Transaction { get; set; }

    public Money Amount { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Optional percentage hint for display purposes. Not used in calculations.
    /// </summary>
    public decimal? Percentage { get; set; }

    public IReadOnlyCollection<TransactionSplitTag> Tags => _tags.AsReadOnly();

    public void AddTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (!_tags.Any(t => t.TagId == tag.Id))
        {
            _tags.Add(new TransactionSplitTag(Id, tag.Id));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTag(Guid tagId)
    {
        var existing = _tags.FirstOrDefault(t => t.TagId == tagId);
        if (existing is not null)
        {
            _tags.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
