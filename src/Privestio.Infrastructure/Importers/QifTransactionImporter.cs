using System.Globalization;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.Importers;

/// <summary>
/// Imports transactions from QIF (Quicken Interchange Format) files.
/// QIF uses single-character line prefixes: D=date, T=amount, P=payee, M=memo, L=category, N=number.
/// Records are separated by ^ characters.
/// </summary>
public class QifTransactionImporter : ITransactionImporter
{
    public string FileFormat => "QIF";

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".qif", StringComparison.OrdinalIgnoreCase);

    public async Task<ImportParseResult> ParseAsync(
        Stream fileStream,
        ImportMapping? mapping = null,
        CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var rows = new List<ImportedTransactionRow>();
        var errors = new List<ImportRowError>();

        var lines = content.Split('\n', StringSplitOptions.TrimEntries);
        var currentFields = new Dictionary<char, string>();
        var rowNumber = 0;

        foreach (var line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(line) || line.StartsWith('!'))
                continue;

            if (line == "^")
            {
                if (currentFields.Count > 0)
                {
                    rowNumber++;
                    try
                    {
                        var row = BuildRow(currentFields);
                        rows.Add(row);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ImportRowError(rowNumber, ex.Message));
                    }
                    currentFields.Clear();
                }
                continue;
            }

            if (line.Length > 1)
            {
                currentFields[line[0]] = line[1..];
            }
        }

        // Handle trailing record without ^
        if (currentFields.Count > 0)
        {
            rowNumber++;
            try
            {
                var row = BuildRow(currentFields);
                rows.Add(row);
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError(rowNumber, ex.Message));
            }
        }

        return new ImportParseResult(rows, errors);
    }

    private static ImportedTransactionRow BuildRow(Dictionary<char, string> fields)
    {
        var dateStr =
            fields.GetValueOrDefault('D') ?? throw new FormatException("Date (D) field is missing");
        var amountStr =
            fields.GetValueOrDefault('T')
            ?? throw new FormatException("Amount (T) field is missing");

        var date = ParseQifDate(dateStr);
        var amount = decimal.Parse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture);
        var payee = fields.GetValueOrDefault('P');
        var memo = fields.GetValueOrDefault('M');
        var category = fields.GetValueOrDefault('L');
        var number = fields.GetValueOrDefault('N');

        return new ImportedTransactionRow(
            Date: date,
            Amount: amount,
            Description: payee ?? memo ?? string.Empty,
            ExternalId: number,
            Payee: payee,
            Category: category,
            Notes: memo
        );
    }

    private static DateTime ParseQifDate(string dateStr)
    {
        // QIF dates can be: MM/DD/YYYY, MM/DD'YY, M/D/YYYY, etc.
        string[] formats = ["M/d/yyyy", "M/d/yy", "MM/dd/yyyy", "MM/dd/yy", "yyyy-MM-dd"];
        if (
            DateTime.TryParseExact(
                dateStr.Trim(),
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed
            )
        )
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        throw new FormatException($"Unrecognized QIF date format: {dateStr}");
    }
}
