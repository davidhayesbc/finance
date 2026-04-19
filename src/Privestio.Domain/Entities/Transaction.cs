using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents an individual financial transaction.
/// When IsSplit is true, budget tracking uses split line categories, not this transaction's category.
/// </summary>
public class Transaction : BaseEntity
{
    private readonly List<TransactionSplit> _splits = [];
    private readonly List<TransactionTag> _tags = [];

    private Transaction() { }

    public Transaction(
        Guid accountId,
        DateTime date,
        Money amount,
        string description,
        TransactionType type
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        AccountId = accountId;
        Date = date;
        Amount = amount;
        Description = description.Trim();
        Type = type;
    }

    public Guid AccountId { get; private set; }
    public Account? Account { get; set; }

    public DateTime Date { get; set; }
    public Money Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionType Type { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid? PayeeId { get; set; }
    public Payee? Payee { get; set; }

    public bool IsReconciled { get; set; }
    public bool IsSplit => _splits.Count > 0;

    public string? Notes { get; set; }
    public string? ExternalId { get; set; }
    public string? ImportFingerprint { get; set; }
    public DateOnly? SettlementDate { get; set; }
    public string? ActivityType { get; set; }
    public string? ActivitySubType { get; set; }
    public string? Direction { get; set; }
    public string? Symbol { get; set; }
    public string? SecurityName { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }

    public Guid? ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }

    /// <summary>
    /// For transfers: the linked counterpart transaction on the destination account.
    /// </summary>
    public Guid? LinkedTransferId { get; set; }

    public IReadOnlyCollection<TransactionSplit> Splits => _splits.AsReadOnly();
    public IReadOnlyCollection<TransactionTag> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Adds a split line. Splits must sum to the parent transaction amount.
    /// </summary>
    public void AddSplit(TransactionSplit split)
    {
        ArgumentNullException.ThrowIfNull(split);
        _splits.Add(split);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearSplits()
    {
        _splits.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the split sum invariant: splits must sum exactly to this transaction's amount
    /// after rounding to minor units (2 decimal places, banker's rounding).
    /// Also validates that all split currencies match the parent transaction currency.
    /// </summary>
    public bool ValidateSplitInvariant()
    {
        if (_splits.Count == 0)
            return true;

        var activeSplits = _splits.Where(s => !s.IsDeleted).ToList();

        // Validate all splits share the parent currency
        if (activeSplits.Any(s => s.Amount.CurrencyCode != Amount.CurrencyCode))
            return false;

        var splitTotal = Math.Round(
            activeSplits.Sum(s => s.Amount.Amount),
            2,
            MidpointRounding.ToEven
        );
        var parentAmount = Math.Round(Amount.Amount, 2, MidpointRounding.ToEven);

        return splitTotal == parentAmount;
    }

    public void AddTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (!_tags.Any(t => t.TagId == tag.Id))
        {
            _tags.Add(new TransactionTag(Id, tag.Id));
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
