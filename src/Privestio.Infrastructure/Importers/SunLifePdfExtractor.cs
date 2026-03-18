using System.Globalization;
using System.Text.RegularExpressions;
using Privestio.Domain.Interfaces;
using UglyToad.PdfPig;

namespace Privestio.Infrastructure.Importers;

/// <summary>
/// Extracts the "My investments" table from Sun Life financial PDF statements
/// using PdfPig's text-with-position extraction.
/// </summary>
internal sealed partial class SunLifePdfExtractor
{
    private const double YTolerance = 3.0;
    private const double HeaderBandTolerance = 20.0;
    private const decimal CrossValidationTolerance = 1.00m;

    private static readonly string[] HeaderKeywords =
    [
        "Investment name",
        "Units",
        "Price",
        "Value",
    ];

    public HoldingsParseResult Extract(Stream pdfStream)
    {
        using var document = PdfDocument.Open(
            pdfStream,
            new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true }
        );

        DateOnly statementDate = default;
        decimal? totalPortfolioValue = null;
        var holdings = new List<ImportedHoldingRow>();
        var errors = new List<ImportRowError>();
        var rowNumber = 0;

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();

            if (statementDate == default)
            {
                statementDate = ExtractStatementDate(words);
            }

            var headerColumns = FindTableHeader(words);
            if (headerColumns is null)
                continue;

            var dataWords = words
                .Where(w => w.BoundingBox.Bottom < headerColumns.HeaderY - YTolerance)
                .ToList();

            var rows = GroupWordsIntoRows(dataWords);

            foreach (var row in rows)
            {
                rowNumber++;

                var nameWords = row.Where(w => w.BoundingBox.Left < headerColumns.UnitsX - 30)
                    .OrderBy(w => w.BoundingBox.Left);
                var name = string.Join(" ", nameWords.Select(w => w.Text)).Trim();

                var metadata = ExtractInstrumentMetadata(name);

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (name.StartsWith("Total", StringComparison.OrdinalIgnoreCase))
                {
                    var totalWords = row.Where(w => w.BoundingBox.Left >= headerColumns.ValueX - 30)
                        .OrderBy(w => w.BoundingBox.Left);
                    var totalText = string.Join("", totalWords.Select(w => w.Text));
                    totalPortfolioValue = ParseDecimal(totalText);
                    break; // everything after Total on this page is comparison tables / admin text
                }

                var unitsText = ExtractColumnText(row, headerColumns.UnitsX, headerColumns.PriceX);
                var priceText = ExtractColumnText(row, headerColumns.PriceX, headerColumns.ValueX);
                var valueText = ExtractColumnText(
                    row,
                    headerColumns.ValueX,
                    headerColumns.ValueX + 150
                );

                if (
                    !TryParseDecimal(unitsText, out var units)
                    || !TryParseDecimal(priceText, out var unitPrice)
                    || !TryParseDecimal(valueText, out var totalValue)
                )
                {
                    // Rows with no numeric data are category separators (e.g. "Balanced", "Fixed income") — skip silently.
                    if (string.IsNullOrEmpty(unitsText) && string.IsNullOrEmpty(priceText) && string.IsNullOrEmpty(valueText))
                        continue;

                    errors.Add(
                        new ImportRowError(
                            rowNumber,
                            $"Failed to parse numeric values: Units='{unitsText}', Price='{priceText}', Value='{valueText}'",
                            $"{name} | {unitsText} | {priceText} | {valueText}"
                        )
                    );
                    continue;
                }

                var computed = units * unitPrice;
                if (Math.Abs(computed - totalValue) >= CrossValidationTolerance)
                {
                    errors.Add(
                        new ImportRowError(
                            rowNumber,
                            $"Cross-validation failed: units ({units}) * price ({unitPrice}) = {computed}, but stated value is {totalValue}",
                            $"{name} | {unitsText} | {priceText} | {valueText}"
                        )
                    );
                    continue;
                }

                holdings.Add(
                    new ImportedHoldingRow(
                        name,
                        units,
                        unitPrice,
                        totalValue,
                        Symbol: metadata.Symbol,
                        Exchange: metadata.Exchange,
                        Cusip: metadata.Cusip,
                        Isin: metadata.Isin
                    )
                );
            }
        }

        return new HoldingsParseResult(
            statementDate,
            holdings,
            errors,
            TotalPortfolioValue: totalPortfolioValue
        );
    }

    private static DateOnly ExtractStatementDate(IReadOnlyList<UglyToad.PdfPig.Content.Word> words)
    {
        var allText = string.Join(" ", words.Select(w => w.Text));

        // Pattern 1: "Statement as of <Month> <Day>, <Year>"
        var match = StatementDateAsOfRegex().Match(allText);
        // Pattern 2: "statement for/period <Month> <Day> to <Month> <Day>, <Year>" — extract end date
        if (!match.Success)
            match = StatementDatePeriodRegex().Match(allText);

        if (
            match.Success
            && DateTime.TryParse(
                match.Groups[1].Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt
            )
        )
        {
            return DateOnly.FromDateTime(dt);
        }

        return default;
    }

    private sealed record ColumnPositions(
        double HeaderY,
        double NameX,
        double UnitsX,
        double PriceX,
        double ValueX
    );

    private static ColumnPositions? FindTableHeader(
        IReadOnlyList<UglyToad.PdfPig.Content.Word> words
    )
    {
        var investmentWords = words
            .Where(w => w.Text.Equals("Investment", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var investmentWord in investmentWords)
        {
            var headerY = investmentWord.BoundingBox.Bottom;
            // PRICE ON / VALUE ON labels appear on a row ~9pt above INVESTMENT NAME / NUMBER OF UNITS,
            // so use a wider band to find all four header keywords.
            var headerBand = words
                .Where(w => Math.Abs(w.BoundingBox.Bottom - headerY) < HeaderBandTolerance)
                .ToList();

            // "NUMBER" is the leftmost word in "NUMBER OF UNITS" and best represents
            // the true left edge of the units column.
            var numberWord = headerBand.FirstOrDefault(w =>
                w.Text.Equals("Number", StringComparison.OrdinalIgnoreCase)
            );
            var unitsWord = headerBand.FirstOrDefault(w =>
                w.Text.Equals("Units", StringComparison.OrdinalIgnoreCase)
            );
            var priceWord = headerBand.FirstOrDefault(w =>
                w.Text.StartsWith("Price", StringComparison.OrdinalIgnoreCase)
            );
            var valueWord = headerBand.FirstOrDefault(w =>
                w.Text.StartsWith("Value", StringComparison.OrdinalIgnoreCase)
            );

            var unitsColumnWord = numberWord ?? unitsWord;
            if (unitsColumnWord is not null && priceWord is not null && valueWord is not null)
            {
                return new ColumnPositions(
                    headerY,
                    investmentWord.BoundingBox.Left,
                    unitsColumnWord.BoundingBox.Left,
                    priceWord.BoundingBox.Left,
                    valueWord.BoundingBox.Left
                );
            }
        }

        return null;
    }

    private static List<List<UglyToad.PdfPig.Content.Word>> GroupWordsIntoRows(
        IReadOnlyList<UglyToad.PdfPig.Content.Word> words
    )
    {
        if (words.Count == 0)
            return [];

        var sorted = words.OrderByDescending(w => w.BoundingBox.Bottom).ToList();
        var rows = new List<List<UglyToad.PdfPig.Content.Word>>();
        var currentRow = new List<UglyToad.PdfPig.Content.Word> { sorted[0] };
        var currentY = sorted[0].BoundingBox.Bottom;

        for (var i = 1; i < sorted.Count; i++)
        {
            var word = sorted[i];
            if (Math.Abs(word.BoundingBox.Bottom - currentY) < YTolerance)
            {
                currentRow.Add(word);
            }
            else
            {
                rows.Add(currentRow);
                currentRow = [word];
                currentY = word.BoundingBox.Bottom;
            }
        }

        if (currentRow.Count > 0)
            rows.Add(currentRow);

        return rows;
    }

    private static string ExtractColumnText(
        IReadOnlyList<UglyToad.PdfPig.Content.Word> row,
        double columnStartX,
        double columnEndX
    )
    {
        // Use a 30pt inward offset so the name/units boundary aligns exactly with the units
        // lower bound. This handles both old PDFs (NUMBER@X≈291, data as far left as X≈265)
        // and new PDFs (NUMBER@X≈245, data starting at X≈253).
        var columnWords = row.Where(w =>
                w.BoundingBox.Left >= columnStartX - 30 && w.BoundingBox.Left < columnEndX - 30
            )
            .OrderBy(w => w.BoundingBox.Left);
        return string.Join("", columnWords.Select(w => w.Text));
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text.Replace("$", "").Replace(",", "").Replace(" ", "").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static decimal? ParseDecimal(string text)
    {
        return TryParseDecimal(text, out var value) ? value : null;
    }

    private static InstrumentMetadata ExtractInstrumentMetadata(string investmentName)
    {
        if (string.IsNullOrWhiteSpace(investmentName))
            return new InstrumentMetadata(null, null, null, null);

        var cusip = ExtractCusipRegex().Match(investmentName).Groups[1].Value;
        if (string.IsNullOrWhiteSpace(cusip))
            cusip = null;

        var isin = ExtractIsinRegex().Match(investmentName).Groups[1].Value;
        if (string.IsNullOrWhiteSpace(isin))
            isin = null;

        string? symbol = null;
        var tickerMatch = ExtractTickerRegex().Match(investmentName);
        if (tickerMatch.Success)
        {
            var candidate = tickerMatch.Groups[1].Value.Trim().ToUpperInvariant();
            if (candidate.Contains('.'))
                symbol = candidate;
        }

        string? exchange = null;
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            var dot = symbol.LastIndexOf('.');
            if (dot > 0 && dot < symbol.Length - 1)
            {
                var suffix = symbol[(dot + 1)..];
                exchange = suffix switch
                {
                    "TO" => "XTSE",
                    "V" => "XTSX",
                    _ => null,
                };
            }
        }

        return new InstrumentMetadata(symbol, exchange, cusip, isin);
    }

    private sealed record InstrumentMetadata(string? Symbol, string? Exchange, string? Cusip, string? Isin);

    [GeneratedRegex(@"Statement\s+as\s+of\s+(\w+\s+\d{1,2},?\s+\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex StatementDateAsOfRegex();

    [GeneratedRegex(
        @"(?:statement\s+for|for\s+the\s+period)\s+\w+.*?\bto\s+(\w+\s+\d{1,2},?\s+\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline
    )]
    private static partial Regex StatementDatePeriodRegex();

    [GeneratedRegex(@"\bCUSIP\s*[:#-]?\s*([0-9A-Z]{9})\b", RegexOptions.IgnoreCase)]
    private static partial Regex ExtractCusipRegex();

    [GeneratedRegex(@"\bISIN\s*[:#-]?\s*([A-Z]{2}[0-9A-Z]{10})\b", RegexOptions.IgnoreCase)]
    private static partial Regex ExtractIsinRegex();

    [GeneratedRegex(@"\b([A-Z]{1,6}(?:\.[A-Z]{1,4})?)\b")]
    private static partial Regex ExtractTickerRegex();
}
