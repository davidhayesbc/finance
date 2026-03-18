using Privestio.Infrastructure.Importers;

namespace Privestio.Infrastructure.Tests.Importers;

public class PdfHoldingsImporterTests
{
    private readonly PdfHoldingsImporter _importer = new();

    [Fact]
    public void FileFormat_ReturnsPdf()
    {
        _importer.FileFormat.Should().Be("PDF");
    }

    [Theory]
    [InlineData("statement.pdf", true)]
    [InlineData("statement.PDF", true)]
    [InlineData("statement.Pdf", true)]
    [InlineData("statement.csv", false)]
    [InlineData("statement.ofx", false)]
    [InlineData("statement.qif", false)]
    [InlineData("statement.txt", false)]
    public void CanHandle_ReturnsExpected(string fileName, bool expected)
    {
        _importer.CanHandle(fileName).Should().Be(expected);
    }
}
