using System.Text;
using Privestio.Domain.Interfaces;
using Privestio.Infrastructure.Importers;

namespace Privestio.Infrastructure.Tests.Importers;

public class OfxTransactionImporterTests
{
    private readonly OfxTransactionImporter _importer = new();

    [Fact]
    public void CanHandle_OfxFile_ReturnsTrue()
    {
        _importer.CanHandle("transactions.ofx").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_QfxFile_ReturnsTrue()
    {
        _importer.CanHandle("transactions.qfx").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_CsvFile_ReturnsFalse()
    {
        _importer.CanHandle("transactions.csv").Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_ValidOfxWithBankTransactions_ParsesRows()
    {
        var ofx = BuildOfxDocument(
            """
            <STMTTRN>
                <TRNTYPE>DEBIT
                <DTPOSTED>20250115120000
                <TRNAMT>-42.99
                <FITID>TXN001
                <NAME>GROCERY STORE
            </STMTTRN>
            <STMTTRN>
                <TRNTYPE>CREDIT
                <DTPOSTED>20250117120000
                <TRNAMT>2500.00
                <FITID>TXN002
                <NAME>PAYROLL DEPOSIT
            </STMTTRN>
            """
        );

        var result = await ParseOfx(ofx);

        result.Rows.Should().HaveCount(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_ParsesAmountCorrectly()
    {
        var ofx = BuildOfxDocument(
            """
            <STMTTRN>
                <TRNTYPE>DEBIT
                <DTPOSTED>20250115120000
                <TRNAMT>-42.99
                <FITID>TXN001
                <NAME>GROCERY STORE
            </STMTTRN>
            """
        );

        var result = await ParseOfx(ofx);

        result.Rows[0].Amount.Should().Be(-42.99m);
        result.Rows[0].Description.Should().Be("GROCERY STORE");
        result.Rows[0].ExternalId.Should().Be("TXN001");
    }

    [Fact]
    public async Task ParseAsync_ParsesDateCorrectly()
    {
        var ofx = BuildOfxDocument(
            """
            <STMTTRN>
                <TRNTYPE>DEBIT
                <DTPOSTED>20250115120000
                <TRNAMT>-10.00
                <FITID>TXN001
                <NAME>TEST
            </STMTTRN>
            """
        );

        var result = await ParseOfx(ofx);

        result.Rows[0].Date.Should().Be(new DateTime(2025, 1, 15, 12, 0, 0));
    }

    [Fact]
    public async Task ParseAsync_UsesNameAsMemoFallback()
    {
        var ofx = BuildOfxDocument(
            """
            <STMTTRN>
                <TRNTYPE>DEBIT
                <DTPOSTED>20250115120000
                <TRNAMT>-10.00
                <FITID>TXN001
                <NAME>CARD PURCHASE
                <MEMO>GROCERY STORE #123 CITY
            </STMTTRN>
            """
        );

        var result = await ParseOfx(ofx);

        // The description should use NAME, and memo should appear in notes
        result.Rows[0].Description.Should().Be("CARD PURCHASE");
        result.Rows[0].Notes.Should().Contain("GROCERY STORE #123 CITY");
    }

    [Fact]
    public async Task ParseAsync_EmptyOfx_ReturnsEmptyResults()
    {
        var ofx = BuildOfxDocument("");

        var result = await ParseOfx(ofx);

        result.Rows.Should().BeEmpty();
    }

    private static string BuildOfxDocument(string transactions) =>
        $"""
            OFXHEADER:100
            DATA:OFXSGML
            VERSION:102
            SECURITY:NONE
            ENCODING:USASCII
            CHARSET:1252
            COMPRESSION:NONE
            OLDFILEUID:NONE
            NEWFILEUID:NONE

            <OFX>
            <SIGNONMSGSRSV1>
            <SONRS>
            <STATUS>
            <CODE>0
            <SEVERITY>INFO
            </STATUS>
            <DTSERVER>20250120120000
            <LANGUAGE>ENG
            </SONRS>
            </SIGNONMSGSRSV1>
            <BANKMSGSRSV1>
            <STMTTRNRS>
            <TRNUID>1001
            <STATUS>
            <CODE>0
            <SEVERITY>INFO
            </STATUS>
            <STMTRS>
            <CURDEF>CAD
            <BANKACCTFROM>
            <BANKID>123456789
            <ACCTID>12345
            <ACCTTYPE>CHECKING
            </BANKACCTFROM>
            <BANKTRANLIST>
            <DTSTART>20250101120000
            <DTEND>20250131120000
            {transactions}
            </BANKTRANLIST>
            </STMTRS>
            </STMTTRNRS>
            </BANKMSGSRSV1>
            </OFX>
            """;

    private async Task<ImportParseResult> ParseOfx(string ofx)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        return await _importer.ParseAsync(stream);
    }
}
