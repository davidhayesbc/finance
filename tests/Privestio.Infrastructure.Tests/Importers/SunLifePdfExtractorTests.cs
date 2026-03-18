using Privestio.Infrastructure.Importers;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Privestio.Infrastructure.Tests.Importers;

public class SunLifePdfExtractorTests
{
    private readonly SunLifePdfExtractor _extractor = new();

    [Fact]
    public void Extract_ValidPdf_ReturnsAllHoldings()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "December 31, 2024",
            holdings:
            [
                ("Sun Life Granite Growth Portfolio", "245.678", "$12.34", "$3,031.47"),
                ("Sun Life MFS International Growth Fund", "100.000", "$15.50", "$1,550.00"),
                ("Sun Life BlackRock Canadian Universe Bond Fund", "500.250", "$9.87", "$4,937.47"),
            ]
        );

        var result = _extractor.Extract(stream);

        result.Holdings.Should().HaveCount(3);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Extract_ValidPdf_ParsesDecimalsCorrectly()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "December 31, 2024",
            holdings: [("Sun Life Granite Growth Portfolio", "245.678", "$12.34", "$3,031.47")]
        );

        var result = _extractor.Extract(stream);

        var holding = result.Holdings[0];
        holding.InvestmentName.Should().Be("Sun Life Granite Growth Portfolio");
        holding.Units.Should().Be(245.678m);
        holding.UnitPrice.Should().Be(12.34m);
        holding.TotalValue.Should().Be(3031.47m);
    }

    [Fact]
    public void Extract_ValidPdf_CrossValidatesAmounts()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "December 31, 2024",
            holdings:
            [
                ("Sun Life Granite Growth Portfolio", "100.000", "$15.50", "$1,550.00"),
                ("Sun Life MFS International Growth Fund", "200.000", "$10.00", "$2,000.00"),
            ]
        );

        var result = _extractor.Extract(stream);

        foreach (var h in result.Holdings)
        {
            var computed = h.Units * h.UnitPrice;
            Math.Abs(computed - h.TotalValue).Should().BeLessThan(1.00m);
        }
    }

    [Fact]
    public void Extract_ValidPdf_ExtractsStatementDate()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "December 31, 2024",
            holdings: [("Test Fund", "100.000", "$10.00", "$1,000.00")]
        );

        var result = _extractor.Extract(stream);

        result.StatementDate.Should().Be(new DateOnly(2024, 12, 31));
    }

    [Fact]
    public void Extract_PdfWithTotalRow_ExtractsTotalPortfolioValue()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "December 31, 2024",
            holdings:
            [
                ("Fund A", "100.000", "$10.00", "$1,000.00"),
                ("Fund B", "200.000", "$5.00", "$1,000.00"),
            ],
            totalValue: "$2,000.00"
        );

        var result = _extractor.Extract(stream);

        result.TotalPortfolioValue.Should().Be(2000.00m);
    }

    [Fact]
    public void Extract_EmptyTable_ReturnsEmptyHoldings()
    {
        using var stream = BuildSunLifePdf(statementDate: "December 31, 2024", holdings: []);

        var result = _extractor.Extract(stream);

        result.Holdings.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Extract_CurrencyDefaultsToCad()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "December 31, 2024",
            holdings: [("Test Fund", "100.000", "$10.00", "$1,000.00")]
        );

        var result = _extractor.Extract(stream);

        result.Currency.Should().Be("CAD");
    }

    [Fact]
    public void Extract_LargeNumbers_ParsesCorrectly()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "March 15, 2025",
            holdings:
            [
                ("Sun Life Granite Balanced Portfolio", "1,234.000", "$123.45", "$152,337.30"),
            ]
        );

        var result = _extractor.Extract(stream);

        result.Holdings.Should().HaveCount(1);
        var holding = result.Holdings[0];
        holding.Units.Should().Be(1234.000m);
        holding.UnitPrice.Should().Be(123.45m);
        holding.TotalValue.Should().Be(152337.30m);
    }

    [Fact]
    public void Extract_VariousDateFormats_ParsesCorrectly()
    {
        using var stream = BuildSunLifePdf(
            statementDate: "January 1, 2025",
            holdings: [("Test Fund", "10.000", "$1.00", "$10.00")]
        );

        var result = _extractor.Extract(stream);

        result.StatementDate.Should().Be(new DateOnly(2025, 1, 1));
    }

    /// <summary>
    /// Builds a synthetic Sun Life PDF with the typical statement layout.
    /// Uses PdfPig's PdfDocumentBuilder to create a real PDF in-memory.
    /// </summary>
    private static MemoryStream BuildSunLifePdf(
        string statementDate,
        List<(string Name, string Units, string Price, string Value)> holdings,
        string? totalValue = null
    )
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        var page = builder.AddPage(612, 792); // Letter size

        // Header
        page.AddText("Sun Life Financial", 16, new PdfPoint(50, 750), boldFont);
        page.AddText($"Statement as of {statementDate}", 10, new PdfPoint(50, 730), font);
        page.AddText("My investments", 14, new PdfPoint(50, 700), boldFont);

        // Table header row
        double nameX = 50;
        double unitsX = 300;
        double priceX = 400;
        double valueX = 500;
        double headerY = 670;

        page.AddText("Investment name", 9, new PdfPoint(nameX, headerY), boldFont);
        page.AddText("Units", 9, new PdfPoint(unitsX, headerY), boldFont);
        page.AddText("Price ($)", 9, new PdfPoint(priceX, headerY), boldFont);
        page.AddText("Value ($)", 9, new PdfPoint(valueX, headerY), boldFont);

        // Table rows
        double rowY = headerY - 20;
        foreach (var (name, units, price, value) in holdings)
        {
            page.AddText(name, 8, new PdfPoint(nameX, rowY), font);
            page.AddText(units, 8, new PdfPoint(unitsX, rowY), font);
            page.AddText(price, 8, new PdfPoint(priceX, rowY), font);
            page.AddText(value, 8, new PdfPoint(valueX, rowY), font);
            rowY -= 15;
        }

        // Total row
        if (totalValue is not null)
        {
            rowY -= 5;
            page.AddText("Total", 9, new PdfPoint(nameX, rowY), boldFont);
            page.AddText(totalValue, 9, new PdfPoint(valueX, rowY), boldFont);
        }

        var ms = new MemoryStream();
        var pdfBytes = builder.Build();
        ms.Write(pdfBytes, 0, pdfBytes.Length);
        ms.Position = 0;
        return ms;
    }
}
