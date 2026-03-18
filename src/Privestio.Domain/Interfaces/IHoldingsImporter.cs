namespace Privestio.Domain.Interfaces;

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
