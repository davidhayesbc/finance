namespace Privestio.Domain.Entities;

/// <summary>
/// Represents a saved column mapping configuration for importing files from a specific institution/format.
/// </summary>
public class ImportMapping : BaseEntity
{
    private ImportMapping() { }

    public ImportMapping(
        string name,
        string fileFormat,
        Guid userId,
        Dictionary<string, string> columnMappings,
        string? institution = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileFormat);
        ArgumentNullException.ThrowIfNull(columnMappings);

        Name = name.Trim();
        FileFormat = fileFormat.Trim().ToUpperInvariant();
        UserId = userId;
        ColumnMappings = columnMappings;
        Institution = institution?.Trim();
    }

    public string Name { get; private set; } = string.Empty;
    public string FileFormat { get; private set; } = string.Empty;
    public string? Institution { get; set; }

    /// <summary>
    /// Maps source file column names to domain field names.
    /// Keys: source column names. Values: target fields (Date, Amount, Description, etc.)
    /// </summary>
    public Dictionary<string, string> ColumnMappings { get; private set; } = new();

    public Guid UserId { get; private set; }
    public User? User { get; set; }

    public string? DateFormat { get; set; }
    public bool HasHeaderRow { get; set; } = true;
    public string? AmountDebitColumn { get; set; }
    public string? AmountCreditColumn { get; set; }

    /// <summary>
    /// Keywords that identify a buy/acquisition in Direction, ActivityType, or ActivitySubType fields.
    /// Matching is case-insensitive substring.
    /// </summary>
    public List<string> BuyKeywords { get; set; } =
    ["buy", "purchase", "contribution", "reinvest", "deposit", "in", "long"];

    /// <summary>
    /// Keywords that identify a sell/disposition in Direction, ActivityType, or ActivitySubType fields.
    /// </summary>
    public List<string> SellKeywords { get; set; } = ["sell", "redeem", "withdraw", "out"];

    /// <summary>
    /// Keywords that identify income credits (dividends, interest, distributions).
    /// </summary>
    public List<string> IncomeKeywords { get; set; } = ["interest", "dividend", "distribution"];

    /// <summary>
    /// Symbol prefixes that identify cash-equivalent holdings (e.g. "CASH" matches CASH, CASHX, CASH.TO).
    /// </summary>
    public List<string> CashEquivalentSymbols { get; set; } = ["CASH"];

    /// <summary>
    /// Row skip patterns: if the Date column value starts with any of these strings, the row is ignored.
    /// Useful for institution-specific footer/metadata rows.
    /// </summary>
    public List<string> IgnoreRowPatterns { get; set; } = [];

    /// <summary>
    /// When true, the parsed amount sign is inverted (for institutions that export debits as positive).
    /// </summary>
    public bool AmountSignFlipped { get; set; }

    public void UpdateMappings(Dictionary<string, string> columnMappings)
    {
        ArgumentNullException.ThrowIfNull(columnMappings);
        ColumnMappings = columnMappings;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
