namespace Privestio.Domain.Interfaces;

/// <summary>
/// Generic import mapping consumed by transaction importer plugins.
/// </summary>
public record TransactionImportMapping(
    IReadOnlyDictionary<string, string> ColumnMappings,
    bool HasHeaderRow = true,
    string? DateFormat = null,
    string? AmountDebitColumn = null,
    string? AmountCreditColumn = null,
    bool AmountSignFlipped = false,
    DateOnly? DefaultDate = null,
    IReadOnlyList<string>? IgnoreRowPatterns = null
);

/// <summary>
/// Represents a parsed row from an imported file before it becomes a Transaction.
/// </summary>
public record ImportedTransactionRow(
    DateTime Date,
    decimal Amount,
    string Description,
    string? ExternalId = null,
    string? Payee = null,
    string? Category = null,
    string? Notes = null,
    DateOnly? SettlementDate = null,
    string? ActivityType = null,
    string? ActivitySubType = null,
    string? Direction = null,
    string? Symbol = null,
    string? SecurityName = null,
    decimal? Quantity = null,
    decimal? UnitPrice = null,
    string? Exchange = null,
    string? Cusip = null,
    string? Isin = null,
    string? Currency = null
);

/// <summary>
/// Result of parsing a file. Contains parsed rows and any per-row errors.
/// </summary>
public record ImportParseResult(
    IReadOnlyList<ImportedTransactionRow> Rows,
    IReadOnlyList<ImportRowError> Errors
);

/// <summary>
/// Represents a row-level error during file parsing.
/// </summary>
public record ImportRowError(int RowNumber, string ErrorMessage, string? RawData = null);

/// <summary>
/// Plugin interface for file-based transaction importers (CSV, OFX, QIF, etc.).
/// </summary>
public interface ITransactionImporter
{
    string FileFormat { get; }
    bool CanHandle(string fileName);
    Task<ImportParseResult> ParseAsync(
        Stream fileStream,
        TransactionImportMapping? mapping = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Represents a single holdings row parsed from a statement file.
/// </summary>
public record ImportedHoldingRow(
    string InvestmentName,
    decimal Units,
    decimal UnitPrice,
    decimal TotalValue,
    string? Symbol = null,
    string? Exchange = null,
    string? Cusip = null,
    string? Isin = null
);

/// <summary>
/// Result of parsing a holdings statement file.
/// </summary>
public record HoldingsParseResult(
    DateOnly StatementDate,
    IReadOnlyList<ImportedHoldingRow> Holdings,
    IReadOnlyList<ImportRowError> Errors,
    string? AccountIdentifier = null,
    decimal? TotalPortfolioValue = null,
    string Currency = "CAD"
);

/// <summary>
/// Plugin interface for file-based holdings importers (PDF statements, etc.).
/// </summary>
public interface IHoldingsImporter
{
    string FileFormat { get; }
    bool CanHandle(string fileName);
    Task<HoldingsParseResult> ParseAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default
    );
}
