using FluentAssertions;
using Privestio.Infrastructure.Importers;
using Xunit;

namespace Privestio.Infrastructure.Tests.Importers;

public class CsvFilePreviewServiceTests
{
    private readonly CsvFilePreviewService _service = new();

    [Fact]
    public async Task PreviewAsync_ValidCsv_ReturnsHeadersAndSampleRows()
    {
        // Arrange
        var csv = "Date,Amount,Description\n2024-01-15,100.00,Groceries\n2024-01-16,50.00,Gas\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _service.PreviewAsync(stream, "test.csv", 10);

        // Assert
        result.FileFormat.Should().Be("CSV");
        result.DetectedColumns.Should().BeEquivalentTo(["Date", "Amount", "Description"]);
        result.SampleRows.Should().HaveCount(2);
        result.TotalRows.Should().Be(2);
        result.SampleRows[0].Should().BeEquivalentTo(["2024-01-15", "100.00", "Groceries"]);
        result.SampleRows[1].Should().BeEquivalentTo(["2024-01-16", "50.00", "Gas"]);
    }

    [Fact]
    public async Task PreviewAsync_LimitsSampleRows()
    {
        // Arrange
        var csv =
            "Date,Amount\n2024-01-01,10\n2024-01-02,20\n2024-01-03,30\n2024-01-04,40\n2024-01-05,50\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _service.PreviewAsync(stream, "test.csv", 3);

        // Assert
        result.SampleRows.Should().HaveCount(3);
        result.TotalRows.Should().Be(5);
    }

    [Fact]
    public async Task PreviewAsync_NonCsvFile_ReturnsEmptyPreview()
    {
        // Arrange
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("some data"));

        // Act
        var result = await _service.PreviewAsync(stream, "test.ofx", 10);

        // Assert
        result.FileFormat.Should().Be("OFX");
        result.DetectedColumns.Should().BeEmpty();
        result.SampleRows.Should().BeEmpty();
        result.TotalRows.Should().Be(0);
    }

    [Fact]
    public async Task PreviewAsync_EmptyCsv_ReturnsHeadersOnly()
    {
        // Arrange
        var csv = "Date,Amount,Description\n";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _service.PreviewAsync(stream, "empty.csv", 10);

        // Assert
        result.DetectedColumns.Should().BeEquivalentTo(["Date", "Amount", "Description"]);
        result.SampleRows.Should().BeEmpty();
        result.TotalRows.Should().Be(0);
    }
}
