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
