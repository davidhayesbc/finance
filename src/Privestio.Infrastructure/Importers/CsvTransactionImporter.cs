using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;

namespace Privestio.Infrastructure.Importers;

/// <summary>
/// Imports transactions from CSV files using dynamic column mapping.
/// </summary>
public class CsvTransactionImporter : ITransactionImporter
{
    public string FileFormat => "CSV";

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<ImportParseResult> ParseAsync(
        Stream fileStream,
        ImportMapping? mapping = null,
        CancellationToken cancellationToken = default
    )
    {
        var rows = new List<ImportedTransactionRow>();
        var errors = new List<ImportRowError>();

        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = mapping?.HasHeaderRow ?? true,
                MissingFieldFound = null,
                BadDataFound = null,
            }
        );

        if (mapping?.HasHeaderRow ?? true)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
        }

        var columnMap = mapping?.ColumnMappings ?? new Dictionary<string, string>();
        var dateFormat = mapping?.DateFormat;
        var debitColumn = mapping?.AmountDebitColumn;
        var creditColumn = mapping?.AmountCreditColumn;

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            try
            {
                var row = ParseRow(csv, columnMap, dateFormat, debitColumn, creditColumn);
                rows.Add(row);
            }
            catch (Exception ex)
            {
                var rawData = csv.Parser?.RawRecord?.TrimEnd('\r', '\n');
                errors.Add(new ImportRowError(rowNumber, ex.Message, rawData));
            }
        }

        return new ImportParseResult(rows, errors);
    }

    private static ImportedTransactionRow ParseRow(
        CsvReader csv,
        Dictionary<string, string> columnMap,
        string? dateFormat,
        string? debitColumn,
        string? creditColumn
    )
    {
        var dateStr =
            GetMappedField(csv, columnMap, "Date")
            ?? throw new FormatException("Date field is missing");

        var date = ParseDate(dateStr, dateFormat);
        var amount = ParseAmount(csv, columnMap, debitColumn, creditColumn);

        var description =
            GetMappedField(csv, columnMap, "Description")
            ?? throw new FormatException("Description field is missing");

        var externalId = GetMappedField(csv, columnMap, "ExternalId");
        var payee = GetMappedField(csv, columnMap, "Payee");
        var category = GetMappedField(csv, columnMap, "Category");
        var notes = GetMappedField(csv, columnMap, "Notes");
        var settlementDate = ParseOptionalDate(
            GetMappedField(csv, columnMap, "SettlementDate"),
            dateFormat
        );
        var activityType = GetMappedField(csv, columnMap, "ActivityType") ?? description;
        var activitySubType = GetMappedField(csv, columnMap, "ActivitySubType");
        var direction = GetMappedField(csv, columnMap, "Direction");
        var symbol = GetMappedField(csv, columnMap, "Symbol");
        var securityName = GetMappedField(csv, columnMap, "SecurityName");
        var quantity = ParseOptionalDecimal(GetMappedField(csv, columnMap, "Quantity"));
        var unitPrice = ParseOptionalDecimal(GetMappedField(csv, columnMap, "UnitPrice"));

        return new ImportedTransactionRow(
            Date: date,
            Amount: amount,
            Description: description,
            ExternalId: externalId,
            Payee: payee,
            Category: category,
            Notes: notes,
            SettlementDate: settlementDate,
            ActivityType: activityType,
            ActivitySubType: activitySubType,
            Direction: direction,
            Symbol: symbol,
            SecurityName: securityName,
            Quantity: quantity,
            UnitPrice: unitPrice
        );
    }

    private static DateTime ParseDate(string dateStr, string? dateFormat)
    {
        DateTime parsed;
        if (!string.IsNullOrEmpty(dateFormat))
        {
            parsed = DateTime.ParseExact(
                dateStr.Trim(),
                dateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None
            );
        }
        else
        {
            parsed = DateTime.Parse(
                dateStr.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None
            );
        }

        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    }

    private static DateOnly? ParseOptionalDate(string? value, string? dateFormat)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parsedDate = ParseDate(value, dateFormat);
        return DateOnly.FromDateTime(parsedDate);
    }

    private static decimal ParseAmount(
        CsvReader csv,
        Dictionary<string, string> columnMap,
        string? debitColumn,
        string? creditColumn
    )
    {
        if (!string.IsNullOrEmpty(debitColumn) && !string.IsNullOrEmpty(creditColumn))
        {
            return ParseDebitCreditAmount(csv, debitColumn, creditColumn);
        }

        var amountStr =
            GetMappedField(csv, columnMap, "Amount")
            ?? throw new FormatException("Amount field is missing");

        return decimal.Parse(amountStr.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
    }

    private static decimal? ParseOptionalDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.Parse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture);
    }

    private static decimal ParseDebitCreditAmount(
        CsvReader csv,
        string debitColumn,
        string creditColumn
    )
    {
        var debitStr = csv.GetField(debitColumn)?.Trim();
        var creditStr = csv.GetField(creditColumn)?.Trim();

        if (
            !string.IsNullOrEmpty(debitStr)
            && decimal.TryParse(
                debitStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var debit
            )
        )
        {
            return -Math.Abs(debit);
        }

        if (
            !string.IsNullOrEmpty(creditStr)
            && decimal.TryParse(
                creditStr,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var credit
            )
        )
        {
            return Math.Abs(credit);
        }

        throw new FormatException("Neither debit nor credit amount could be parsed");
    }

    private static string? GetMappedField(
        CsvReader csv,
        Dictionary<string, string> columnMap,
        string targetField
    )
    {
        var sourceColumn = columnMap
            .Where(kvp => kvp.Value.Equals(targetField, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (sourceColumn is null)
            return null;

        var value = csv.GetField(sourceColumn);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
