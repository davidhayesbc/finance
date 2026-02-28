using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a financial account (banking, investment, credit, property, or loan).
/// CurrentBalance is a computed/cached value derived from account type rules.
/// </summary>
public class Account : BaseEntity
{
    private readonly List<Transaction> _transactions = [];
    private readonly List<Valuation> _valuations = [];

    private Account() { }

    public Account(
        string name,
        AccountType accountType,
        AccountSubType accountSubType,
        string currency,
        Money openingBalance,
        DateTime openingDate,
        Guid ownerId,
        string? institution = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        Name = name.Trim();
        AccountType = accountType;
        AccountSubType = accountSubType;
        Currency = currency.ToUpperInvariant();
        OpeningBalance = openingBalance;
        OpeningDate = openingDate;
        OwnerId = ownerId;
        Institution = institution?.Trim();
    }

    public string Name { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public AccountSubType AccountSubType { get; private set; }
    public string Currency { get; private set; } = "CAD";
    public string? Institution { get; set; }
    public string? AccountNumber { get; set; }
    public Money OpeningBalance { get; private set; }
    public DateTime OpeningDate { get; private set; }

    /// <summary>
    /// Cached computed balance. Updated by the infrastructure layer.
    /// Never set directly by user input.
    /// </summary>
    public Money CurrentBalance { get; set; }

    public bool IsShared { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public Guid OwnerId { get; private set; }
    public User? Owner { get; set; }

    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();
    public IReadOnlyCollection<Valuation> Valuations => _valuations.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the latest valuation for property/asset accounts.
    /// </summary>
    public Valuation? GetLatestValuation() =>
        _valuations
            .Where(v => !v.IsDeleted)
            .OrderByDescending(v => v.EffectiveDate)
            .FirstOrDefault();
}
