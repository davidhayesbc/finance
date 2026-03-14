using System.Text;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;
using Privestio.Infrastructure.Importers;

namespace Privestio.Infrastructure.Tests.Importers;

public class CsvTransactionImporterTests
{
    private readonly CsvTransactionImporter _importer = new();

    [Fact]
    public void CanHandle_CsvFile_ReturnsTrue()
    {
        _importer.CanHandle("transactions.csv").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_CsvUpperCase_ReturnsTrue()
    {
        _importer.CanHandle("TRANSACTIONS.CSV").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_OfxFile_ReturnsFalse()
    {
        _importer.CanHandle("transactions.ofx").Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_StandardCsv_ParsesAllRows()
    {
        var csv = """
            Date,Amount,Description
            2025-01-15,-42.99,GROCERY STORE
            2025-01-16,-15.50,GAS STATION
            2025-01-17,2500.00,PAYROLL DEPOSIT
            """;

        var mapping = CreateMapping(
            new()
            {
                { "Date", "Date" },
                { "Amount", "Amount" },
                { "Description", "Description" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        result.Rows.Should().HaveCount(3);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_StandardCsv_ParsesValuesCorrectly()
    {
        var csv = """
            Date,Amount,Description
            2025-01-15,-42.99,GROCERY STORE
            """;

        var mapping = CreateMapping(
            new()
            {
                { "Date", "Date" },
                { "Amount", "Amount" },
                { "Description", "Description" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        var row = result.Rows[0];
        row.Date.Should().Be(new DateTime(2025, 1, 15));
        row.Amount.Should().Be(-42.99m);
        row.Description.Should().Be("GROCERY STORE");
    }

    [Fact]
    public async Task ParseAsync_SeparateDebitCreditColumns_ParsesCorrectly()
    {
        var csv = """
            Date,Debit,Credit,Description
            2025-01-15,42.99,,GROCERY STORE
            2025-01-17,,2500.00,PAYROLL DEPOSIT
            """;

        var mapping = CreateMapping(
            new() { { "Date", "Date" }, { "Description", "Description" } },
            amountDebitColumn: "Debit",
            amountCreditColumn: "Credit"
        );

        var result = await ParseCsv(csv, mapping);

        result.Rows.Should().HaveCount(2);
        result.Rows[0].Amount.Should().Be(-42.99m);
        result.Rows[1].Amount.Should().Be(2500.00m);
    }

    [Fact]
    public async Task ParseAsync_CustomDateFormat_ParsesCorrectly()
    {
        var csv = """
            Date,Amount,Description
            01/15/2025,-42.99,GROCERY STORE
            """;

        var mapping = CreateMapping(
            new()
            {
                { "Date", "Date" },
                { "Amount", "Amount" },
                { "Description", "Description" },
            },
            dateFormat: "MM/dd/yyyy"
        );

        var result = await ParseCsv(csv, mapping);

        result.Rows[0].Date.Should().Be(new DateTime(2025, 1, 15));
    }

    [Fact]
    public async Task ParseAsync_InvalidRow_ReportsError()
    {
        var csv = """
            Date,Amount,Description
            2025-01-15,-42.99,GROCERY STORE
            BAD-DATE,not-a-number,BROKEN ROW
            2025-01-17,2500.00,PAYROLL DEPOSIT
            """;

        var mapping = CreateMapping(
            new()
            {
                { "Date", "Date" },
                { "Amount", "Amount" },
                { "Description", "Description" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        result.Rows.Should().HaveCount(2);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RowNumber.Should().Be(3);
    }

    [Fact]
    public async Task ParseAsync_WithOptionalColumns_ParsesPayeeAndNotes()
    {
        var csv = """
            Date,Amount,Description,Payee,Notes
            2025-01-15,-42.99,POS PURCHASE,SuperMart,Weekly groceries
            """;

        var mapping = CreateMapping(
            new()
            {
                { "Date", "Date" },
                { "Amount", "Amount" },
                { "Description", "Description" },
                { "Payee", "Payee" },
                { "Notes", "Notes" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        result.Rows[0].Payee.Should().Be("SuperMart");
        result.Rows[0].Notes.Should().Be("Weekly groceries");
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsEmptyResults()
    {
        var csv = "Date,Amount,Description\n";

        var mapping = CreateMapping(
            new()
            {
                { "Date", "Date" },
                { "Amount", "Amount" },
                { "Description", "Description" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        result.Rows.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithExternalIdColumn_ParsesExternalId()
    {
        var csv = """
            Date,Amount,Description,RefNum
            2025-01-15,-42.99,GROCERY STORE,TXN-001
            """;

        var mapping = CreateMapping(
            new()
            {
                { "Date", "Date" },
                { "Amount", "Amount" },
                { "Description", "Description" },
                { "RefNum", "ExternalId" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        result.Rows[0].ExternalId.Should().Be("TXN-001");
    }

    [Fact]
    public async Task ParseAsync_WealthsimpleTradeColumns_ParsesInvestmentMetadata()
    {
        var csv = """
            transaction_date,settlement_date,activity_type,activity_sub_type,direction,symbol,name,quantity,unit_price,net_cash_amount
            2025-02-03,2025-02-03,Trade,BUY,LONG,XEQT,iShares Core Equity ETF Portfolio,2.1361,35.1098,-75
            """;

        var mapping = CreateMapping(
            new()
            {
                { "transaction_date", "Date" },
                { "net_cash_amount", "Amount" },
                { "activity_type", "Description" },
                { "settlement_date", "SettlementDate" },
                { "activity_sub_type", "ActivitySubType" },
                { "direction", "Direction" },
                { "symbol", "Symbol" },
                { "name", "SecurityName" },
                { "quantity", "Quantity" },
                { "unit_price", "UnitPrice" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        result.Errors.Should().BeEmpty();
        result.Rows.Should().HaveCount(1);

        var row = result.Rows[0];
        row.Description.Should().Be("Trade");
        row.SettlementDate.Should().Be(new DateOnly(2025, 2, 3));
        row.ActivityType.Should().Be("Trade");
        row.ActivitySubType.Should().Be("BUY");
        row.Direction.Should().Be("LONG");
        row.Symbol.Should().Be("XEQT");
        row.SecurityName.Should().Be("iShares Core Equity ETF Portfolio");
        row.Quantity.Should().Be(2.1361m);
        row.UnitPrice.Should().Be(35.1098m);
    }

    [Fact]
    public async Task ParseAsync_WealthsimpleFooterRow_IgnoredWithoutError()
    {
        var csv = """
            transaction_date,settlement_date,activity_type,activity_sub_type,direction,symbol,name,quantity,unit_price,net_cash_amount
            2025-02-03,2025-02-03,Trade,BUY,LONG,XEQT,iShares Core Equity ETF Portfolio,2.1361,35.1098,-75
            As of 2026-03-13 16:02 GMT-07:00,,,,,,,,,
            """;

        var mapping = CreateMapping(
            new()
            {
                { "transaction_date", "Date" },
                { "net_cash_amount", "Amount" },
                { "activity_type", "Description" },
                { "settlement_date", "SettlementDate" },
                { "activity_sub_type", "ActivitySubType" },
                { "direction", "Direction" },
                { "symbol", "Symbol" },
                { "name", "SecurityName" },
                { "quantity", "Quantity" },
                { "unit_price", "UnitPrice" },
            }
        );

        var result = await ParseCsv(csv, mapping);

        result.Errors.Should().BeEmpty();
        result.Rows.Should().HaveCount(1);
    }

    private static ImportMapping CreateMapping(
        Dictionary<string, string> columnMappings,
        string? dateFormat = null,
        string? amountDebitColumn = null,
        string? amountCreditColumn = null
    )
    {
        var mapping = new ImportMapping("Test", "CSV", Guid.NewGuid(), columnMappings);
        mapping.DateFormat = dateFormat;
        mapping.AmountDebitColumn = amountDebitColumn;
        mapping.AmountCreditColumn = amountCreditColumn;
        return mapping;
    }

    private async Task<ImportParseResult> ParseCsv(string csv, ImportMapping mapping)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return await _importer.ParseAsync(stream, mapping);
    }
}
