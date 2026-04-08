using System.Globalization;
using System.Text.RegularExpressions;
using Privestio.Domain.Interfaces;
using UglyToad.PdfPig;

namespace Privestio.Infrastructure.Importers;

/// <summary>
/// Extracts the "Account Details" holdings table from Worldsource Financial Management
/// quarterly PDF statements using PdfPig's text-with-position extraction.
/// </summary>
internal sealed partial class WorldsourcePdfExtractor
{
    private const double YTolerance = 3.0;
    private const double HeaderBandTolerance = 15.0;
    private const decimal CrossValidationTolerance = 1.00m;

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

            // The Worldsource header spans two rows (e.g. "Investment" / "MutualFund" + "Book" / "Value").
            // Exclude everything within the header band so sub-header labels are not treated as data.
            var dataWords = words
                .Where(w =>
                    w.BoundingBox.Bottom < headerColumns.HeaderY - HeaderBandTolerance - YTolerance
                )
                .ToList();

            var rows = GroupWordsIntoRows(dataWords);

            foreach (var row in rows)
            {
                rowNumber++;

                var nameWords = row.Where(w => w.BoundingBox.Left < headerColumns.SymbolX - 10)
                    .OrderBy(w => w.BoundingBox.Left);
                var name = string.Join(" ", nameWords.Select(w => w.Text)).Trim();

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // Skip DSC/LSC/LL lines (deferred sales charge annotations)
                if (IsSalesChargeAnnotation(name))
                    continue;

                // Skip footer rows (company contact info, address, etc.)
                if (IsFooterRow(name))
                    continue;

                // Total row detection
                if (name.StartsWith("Total", StringComparison.OrdinalIgnoreCase))
                {
                    var totalWords = row.Where(w =>
                            w.BoundingBox.Left >= headerColumns.MarketValueX - 30
                        )
                        .OrderBy(w => w.BoundingBox.Left);
                    var totalText = string.Join("", totalWords.Select(w => w.Text));
                    totalPortfolioValue = ParseDecimal(totalText);
                    continue;
                }

                // Subtotal row (just dollar amounts, no name besides the total)
                if (name.StartsWith("$", StringComparison.OrdinalIgnoreCase))
                    continue;

                var symbolText = ExtractColumnText(
                    row,
                    headerColumns.SymbolX,
                    headerColumns.AccountNumberX
                );
                var quantityText = ExtractColumnText(
                    row,
                    headerColumns.QuantityX,
                    headerColumns.PriceX
                );
                var priceText = SplitFirstDollarAmount(
                    ExtractColumnText(
                        row,
                        headerColumns.PriceX,
                        headerColumns.BookValueX
                    )
                );
                var marketValueText = ExtractColumnText(
                    row,
                    headerColumns.MarketValueX,
                    headerColumns.MarketValueX + 150
                );

                if (
                    !TryParseDecimal(quantityText, out var units)
                    || !TryParseDecimal(priceText, out var unitPrice)
                    || !TryParseDecimal(marketValueText, out var totalValue)
                )
                {
                    // Rows with no numeric data are section separators — skip silently
                    if (
                        string.IsNullOrEmpty(quantityText)
                        && string.IsNullOrEmpty(priceText)
                        && string.IsNullOrEmpty(marketValueText)
                    )
                        continue;

                    errors.Add(
                        new ImportRowError(
                            rowNumber,
                            $"Failed to parse numeric values: Quantity='{quantityText}', Price='{priceText}', MarketValue='{marketValueText}'",
                            $"{name} | {symbolText} | {quantityText} | {priceText} | {marketValueText}"
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
                            $"Cross-validation failed: quantity ({units}) * price ({unitPrice}) = {computed}, but stated market value is {totalValue}",
                            $"{name} | {symbolText} | {quantityText} | {priceText} | {marketValueText}"
                        )
                    );
                    continue;
                }

                holdings.Add(
                    new ImportedHoldingRow(
                        CleanInvestmentName(name),
                        units,
                        unitPrice,
                        totalValue,
                        Symbol: string.IsNullOrWhiteSpace(symbolText) ? null : symbolText.Trim()
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

    /// <summary>
    /// Detects whether a PDF contains a Worldsource statement by looking for identifying text.
    /// </summary>
    public static bool IsWorldsourcePdf(Stream pdfStream)
    {
        var position = pdfStream.Position;
        try
        {
            using var document = PdfDocument.Open(
                pdfStream,
                new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true }
            );

            foreach (var page in document.GetPages().Take(2))
            {
                var text = string.Join(
                    " ",
                    page.GetWords().Select(w => w.Text)
                );
                if (
                    text.Contains("Worldsource", StringComparison.OrdinalIgnoreCase)
                    && text.Contains("Financial", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (pdfStream.CanSeek)
                pdfStream.Position = position;
        }
    }

    private static DateOnly ExtractStatementDate(IReadOnlyList<UglyToad.PdfPig.Content.Word> words)
    {
        var allText = string.Join(" ", words.Select(w => w.Text));

        // Pattern: "For the period <Month> <Day>, <Year> to <Month> <Day>, <Year>" — extract end date
        var match = PeriodEndDateRegex().Match(allText);

        // Fallback: "As of <Month> <Day>, <Year>"
        if (!match.Success)
            match = AsOfDateRegex().Match(allText);

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
        double InvestmentX,
        double SymbolX,
        double AccountNumberX,
        double QuantityX,
        double PriceX,
        double BookValueX,
        double MarketValueX
    );

    private static ColumnPositions? FindTableHeader(
        IReadOnlyList<UglyToad.PdfPig.Content.Word> words
    )
    {
        // Look for the word "Investment" as part of the column header
        // Worldsource headers: Investment | Symbol | AccountNumber | Quantity | Price | BookValue | MarketValue
        // Actual header labels span two rows:
        //   Row 1: "Investment" ... "Symbol" ... "AccountNumber" ... "Quantity" ... "Price" ... "Book" ... "Market"
        //   Row 2: "MutualFund"                                                               "Value" ... "Value"
        var investmentWords = words
            .Where(w => w.Text.Equals("Investment", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var investmentWord in investmentWords)
        {
            var headerY = investmentWord.BoundingBox.Bottom;
            var headerBand = words
                .Where(w => Math.Abs(w.BoundingBox.Bottom - headerY) < HeaderBandTolerance)
                .ToList();

            var symbolWord = headerBand.FirstOrDefault(w =>
                w.Text.Equals("Symbol", StringComparison.OrdinalIgnoreCase)
            );
            var quantityWord = headerBand.FirstOrDefault(w =>
                w.Text.Equals("Quantity", StringComparison.OrdinalIgnoreCase)
            );
            var priceWord = headerBand.FirstOrDefault(w =>
                w.Text.Equals("Price", StringComparison.OrdinalIgnoreCase)
            );

            // "AccountNumber" may appear as one word or "Account" + "Number"
            var accountNumberWord = headerBand.FirstOrDefault(w =>
                    w.Text.StartsWith("Account", StringComparison.OrdinalIgnoreCase)
                        && w.Text.Contains("Number", StringComparison.OrdinalIgnoreCase)
                )
                ?? headerBand.FirstOrDefault(w =>
                    w.Text.Equals("Account", StringComparison.OrdinalIgnoreCase)
                );

            // Look for "Book" and "Market" in the header band
            var bookWord = headerBand.FirstOrDefault(w =>
                w.Text.Equals("Book", StringComparison.OrdinalIgnoreCase)
            );
            var marketWord = headerBand.FirstOrDefault(w =>
                w.Text.Equals("Market", StringComparison.OrdinalIgnoreCase)
            );

            if (
                symbolWord is not null
                && quantityWord is not null
                && priceWord is not null
                && marketWord is not null
            )
            {
                return new ColumnPositions(
                    headerY,
                    investmentWord.BoundingBox.Left,
                    symbolWord.BoundingBox.Left,
                    accountNumberWord?.BoundingBox.Left ?? symbolWord.BoundingBox.Left + 80,
                    quantityWord.BoundingBox.Left,
                    priceWord.BoundingBox.Left,
                    bookWord?.BoundingBox.Left ?? priceWord.BoundingBox.Left + 60,
                    marketWord.BoundingBox.Left
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
        var columnWords = row.Where(w =>
                w.BoundingBox.Left >= columnStartX - 20 && w.BoundingBox.Left < columnEndX - 20
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

    private static bool IsSalesChargeAnnotation(string text)
    {
        var trimmed = text.Trim().ToUpperInvariant();
        return trimmed is "DSC" or "LSC" or "LL" or "FE";
    }

    private static bool IsFooterRow(string name) =>
        name.Contains("Worldsource Financial", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Cochrane", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// When PdfPig merges two adjacent dollar amounts into one word (e.g., "$12.2166$213,880.44"),
    /// extract only the first dollar amount.
    /// </summary>
    private static string SplitFirstDollarAmount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var secondDollar = text.IndexOf('$', 1);
        return secondDollar > 0 ? text[..secondDollar] : text;
    }

    private static string CleanInvestmentName(string name)
    {
        // Worldsource concatenates words without spaces; normalize common patterns
        return name.Trim();
    }

    [GeneratedRegex(
        @"(?:for\s*the\s*period|period)\s+\w+.*?\bto\s*(\w+\s+\d{1,2},?\s+\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Singleline
    )]
    private static partial Regex PeriodEndDateRegex();

    [GeneratedRegex(
        @"[Aa]s\s+of\s*(\w+\s+\d{1,2},?\s+\d{4})",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex AsOfDateRegex();
}
