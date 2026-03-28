using System.Globalization;
using System.Text.RegularExpressions;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.Importers;

/// <summary>
/// Imports transactions from OFX/QFX files (SGML-based format used by banks).
/// </summary>
public partial class OfxTransactionImporter : ITransactionImporter
{
    public string FileFormat => "OFX";

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".ofx", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".qfx", StringComparison.OrdinalIgnoreCase);

    public async Task<ImportParseResult> ParseAsync(
        Stream fileStream,
        TransactionImportMapping? mapping = null,
        CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var rows = new List<ImportedTransactionRow>();
        var errors = new List<ImportRowError>();

        var transactionMatches = StmtTrnRegex().Matches(content);
        var rowNumber = 0;

        foreach (Match match in transactionMatches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            try
            {
                var block = match.Groups[1].Value;
                var row = ParseTransaction(block);
                rows.Add(row);
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError(rowNumber, ex.Message, match.Value.Trim()));
            }
        }

        return new ImportParseResult(rows, errors);
    }

    private static ImportedTransactionRow ParseTransaction(string block)
    {
        var dateStr =
            GetTagValue(block, "DTPOSTED") ?? throw new FormatException("DTPOSTED is missing");
        var amountStr =
            GetTagValue(block, "TRNAMT") ?? throw new FormatException("TRNAMT is missing");
        var name = GetTagValue(block, "NAME");
        var memo = GetTagValue(block, "MEMO");
        var fitId = GetTagValue(block, "FITID");

        var date = ParseOfxDate(dateStr);
        var amount = decimal.Parse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture);
        var description = name ?? memo ?? throw new FormatException("NAME or MEMO is required");

        return new ImportedTransactionRow(
            Date: date,
            Amount: amount,
            Description: description,
            ExternalId: fitId,
            Notes: name is not null && memo is not null ? memo : null
        );
    }

    private static DateTime ParseOfxDate(string dateStr)
    {
        // OFX dates: YYYYMMDDHHMMSS or YYYYMMDD or YYYYMMDDHHMMSS.XXX[TZ]
        var cleaned = dateStr.Split('[')[0].Split('.')[0].Trim();

        var parsed = cleaned.Length switch
        {
            8 => DateTime.ParseExact(cleaned, "yyyyMMdd", CultureInfo.InvariantCulture),
            14 => DateTime.ParseExact(cleaned, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            _ => throw new FormatException($"Unrecognized OFX date format: {dateStr}"),
        };

        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    }

    private static string? GetTagValue(string block, string tagName)
    {
        var match = Regex.Match(block, $@"<{tagName}>([^<\r\n]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex(@"<STMTTRN>(.*?)</STMTTRN>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex StmtTrnRegex();
}
