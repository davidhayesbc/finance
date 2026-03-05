using System.Text;
using Privestio.Domain.Interfaces;
using Privestio.Infrastructure.Importers;

namespace Privestio.Infrastructure.Tests.Importers;

public class QifTransactionImporterTests
{
    private readonly QifTransactionImporter _importer = new();

    [Fact]
    public void CanHandle_QifFile_ReturnsTrue()
    {
        _importer.CanHandle("export.qif").Should().BeTrue();
    }

    [Fact]
    public void CanHandle_CsvFile_ReturnsFalse()
    {
        _importer.CanHandle("export.csv").Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_SingleTransaction_ParsesCorrectly()
    {
        var qif = """
            !Type:Bank
            D01/15/2025
            T-42.99
            PGROCERY STORE
            ^
            """;

        var result = await ParseQif(qif);

        result.Rows.Should().HaveCount(1);
        result.Rows[0].Date.Should().Be(new DateTime(2025, 1, 15));
        result.Rows[0].Amount.Should().Be(-42.99m);
        result.Rows[0].Description.Should().Be("GROCERY STORE");
    }

    [Fact]
    public async Task ParseAsync_MultipleTransactions_ParsesAll()
    {
        var qif = """
            !Type:Bank
            D01/15/2025
            T-42.99
            PGROCERY STORE
            ^
            D01/17/2025
            T2500.00
            PPAYROLL DEPOSIT
            ^
            """;

        var result = await ParseQif(qif);

        result.Rows.Should().HaveCount(2);
        result.Rows[0].Amount.Should().Be(-42.99m);
        result.Rows[1].Amount.Should().Be(2500.00m);
    }

    [Fact]
    public async Task ParseAsync_WithMemo_ParsesMemoAsNotes()
    {
        var qif = """
            !Type:Bank
            D01/15/2025
            T-10.00
            PCOFFEE SHOP
            MMonday morning coffee
            ^
            """;

        var result = await ParseQif(qif);

        result.Rows[0].Notes.Should().Be("Monday morning coffee");
    }

    [Fact]
    public async Task ParseAsync_WithCategory_ParsesCategory()
    {
        var qif = """
            !Type:Bank
            D01/15/2025
            T-42.99
            PGROCERY STORE
            LFood:Groceries
            ^
            """;

        var result = await ParseQif(qif);

        result.Rows[0].Category.Should().Be("Food:Groceries");
    }

    [Fact]
    public async Task ParseAsync_WithNumber_ParsesAsExternalId()
    {
        var qif = """
            !Type:Bank
            D01/15/2025
            T-42.99
            PGROCERY STORE
            N12345
            ^
            """;

        var result = await ParseQif(qif);

        result.Rows[0].ExternalId.Should().Be("12345");
    }

    [Fact]
    public async Task ParseAsync_MissingPayee_UsesBlankDescription()
    {
        var qif = """
            !Type:Bank
            D01/15/2025
            T-42.99
            ^
            """;

        var result = await ParseQif(qif);

        // Transaction should still parse - payee line is optional
        result.Rows.Should().HaveCount(1);
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsEmptyResults()
    {
        var result = await ParseQif("");

        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_InvalidDate_CollectsError()
    {
        var qif = """
            !Type:Bank
            Dnot-a-date
            T-42.99
            PGROCERY STORE
            ^
            """;

        var result = await ParseQif(qif);

        result.Rows.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RowNumber.Should().Be(1);
    }

    private async Task<ImportParseResult> ParseQif(string qif)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(qif));
        return await _importer.ParseAsync(stream);
    }
}
