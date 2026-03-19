using Privestio.Domain.Entities;

namespace Privestio.Domain.Interfaces;

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
        ImportMapping? mapping = null,
        CancellationToken cancellationToken = default
    );
}
