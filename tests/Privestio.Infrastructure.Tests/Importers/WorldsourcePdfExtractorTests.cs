using Privestio.Infrastructure.Importers;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Privestio.Infrastructure.Tests.Importers;

public class WorldsourcePdfExtractorTests
{
    private readonly WorldsourcePdfExtractor _extractor = new();

    [Fact]
    public void Extract_ValidPdf_ReturnsHolding()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: "December 31, 2025",
            holdings:
            [
                (
                    "MANULIFE GLOBAL FRANCHISE FUND",
                    "MMF3433",
                    "16069956001",
                    "21,701.9100",
                    "$13.7356",
                    "$254,019.32",
                    "$298,088.75"
                ),
            ]
        );

        var result = _extractor.Extract(stream);

        result.Holdings.Should().HaveCount(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Extract_ValidPdf_ParsesDecimalsCorrectly()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: "December 31, 2025",
            holdings:
            [
                (
                    "MANULIFE GLOBAL FRANCHISE FUND",
                    "MMF3433",
                    "16069956001",
                    "21,701.9100",
                    "$13.7356",
                    "$254,019.32",
                    "$298,088.75"
                ),
            ]
        );

        var result = _extractor.Extract(stream);

        var holding = result.Holdings[0];
        holding.InvestmentName.Should().Be("MANULIFE GLOBAL FRANCHISE FUND");
        holding.Units.Should().Be(21701.9100m);
        holding.UnitPrice.Should().Be(13.7356m);
        holding.TotalValue.Should().Be(298088.75m);
        holding.Symbol.Should().Be("MMF3433");
    }

    [Fact]
    public void Extract_ValidPdf_CrossValidatesAmounts()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: "March 31, 2023",
            holdings:
            [
                (
                    "MANULIFE GLOBAL FRANCHISE FUND",
                    "MMF3433",
                    "16069956001",
                    "18,567.6560",
                    "$12.2166",
                    "$213,880.44",
                    "$226,833.63"
                ),
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
        using var stream = BuildWorldsourcePdf(
            periodEnd: "December 31, 2025",
            holdings:
            [
                (
                    "Test Fund",
                    "TST001",
                    "12345",
                    "100.0000",
                    "$10.0000",
                    "$1,000.00",
                    "$1,000.00"
                ),
            ]
        );

        var result = _extractor.Extract(stream);

        result.StatementDate.Should().Be(new DateOnly(2025, 12, 31));
    }

    [Fact]
    public void Extract_PdfWithTotalRow_ExtractsTotalPortfolioValue()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: "December 31, 2025",
            holdings:
            [
                (
                    "Fund A",
                    "FA001",
                    "11111",
                    "100.0000",
                    "$10.0000",
                    "$1,000.00",
                    "$1,000.00"
                ),
            ],
            totalBookValue: "$1,000.00",
            totalMarketValue: "$1,000.00"
        );

        var result = _extractor.Extract(stream);

        result.TotalPortfolioValue.Should().Be(1000.00m);
    }

    [Fact]
    public void Extract_EmptyTable_ReturnsEmptyHoldings()
    {
        using var stream = BuildWorldsourcePdf(periodEnd: "December 31, 2025", holdings: []);

        var result = _extractor.Extract(stream);

        result.Holdings.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Extract_CurrencyDefaultsToCad()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: "December 31, 2025",
            holdings:
            [
                (
                    "Test Fund",
                    "TST001",
                    "12345",
                    "100.0000",
                    "$10.0000",
                    "$1,000.00",
                    "$1,000.00"
                ),
            ]
        );

        var result = _extractor.Extract(stream);

        result.Currency.Should().Be("CAD");
    }

    [Fact]
    public void Extract_DscAnnotation_IsSkipped()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: "December 31, 2025",
            holdings:
            [
                (
                    "MANULIFE GLOBAL FRANCHISE FUND",
                    "MMF3433",
                    "16069956001",
                    "21,701.9100",
                    "$13.7356",
                    "$254,019.32",
                    "$298,088.75"
                ),
            ],
            includeDscAnnotation: true
        );

        var result = _extractor.Extract(stream);

        result.Holdings.Should().HaveCount(1);
        result.Holdings[0].InvestmentName.Should().NotBe("DSC");
    }

    [Fact]
    public void Extract_AsOfDateFormat_ParsesCorrectly()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: null,
            asOfDate: "December 31, 2022",
            holdings:
            [
                (
                    "Test Fund",
                    "TST001",
                    "12345",
                    "100.0000",
                    "$10.0000",
                    "$1,000.00",
                    "$1,000.00"
                ),
            ]
        );

        var result = _extractor.Extract(stream);

        result.StatementDate.Should().Be(new DateOnly(2022, 12, 31));
    }

    [Fact]
    public void IsWorldsourcePdf_WorldsourcePdf_ReturnsTrue()
    {
        using var stream = BuildWorldsourcePdf(
            periodEnd: "December 31, 2025",
            holdings: []
        );

        var result = WorldsourcePdfExtractor.IsWorldsourcePdf(stream);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsWorldsourcePdf_NonWorldsourcePdf_ReturnsFalse()
    {
        // Build a Sun Life-style PDF
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(612, 792);
        page.AddText("Sun Life Financial", 12, new PdfPoint(50, 700), font);
        page.AddText("Statement as of December 31, 2025", 10, new PdfPoint(50, 680), font);

        var ms = new MemoryStream();
        var pdfBytes = builder.Build();
        ms.Write(pdfBytes, 0, pdfBytes.Length);
        ms.Position = 0;

        var result = WorldsourcePdfExtractor.IsWorldsourcePdf(ms);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Builds a synthetic Worldsource PDF with the typical quarterly statement layout.
    /// </summary>
    private static MemoryStream BuildWorldsourcePdf(
        string? periodEnd,
        List<(
            string Name,
            string Symbol,
            string AccountNumber,
            string Quantity,
            string Price,
            string BookValue,
            string MarketValue
        )> holdings,
        string? asOfDate = null,
        string? totalBookValue = null,
        string? totalMarketValue = null,
        bool includeDscAnnotation = false
    )
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var boldFont = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        // Page 1: Header page with Worldsource branding
        var page1 = builder.AddPage(612, 792);
        if (periodEnd is not null)
        {
            page1.AddText(
                $"For the period October 1, 2025 to {periodEnd}",
                10,
                new PdfPoint(50, 750),
                font
            );
        }
        else if (asOfDate is not null)
        {
            page1.AddText(
                $"As of {asOfDate}",
                10,
                new PdfPoint(50, 750),
                font
            );
        }

        page1.AddText(
            "Worldsource Financial Management Inc.",
            12,
            new PdfPoint(50, 700),
            boldFont
        );
        page1.AddText("Investor Number: 487161", 10, new PdfPoint(50, 680), font);

        // Page 2: Portfolio Account Details with holdings table
        var page2 = builder.AddPage(612, 792);
        page2.AddText("Portfolio Account Details", 14, new PdfPoint(50, 750), boldFont);
        page2.AddText("Account Details: RRSP", 10, new PdfPoint(50, 710), boldFont);

        // Column header row
        double investmentX = 30;
        double symbolX = 195;
        double accountNumberX = 255;
        double quantityX = 340;
        double priceX = 405;
        double bookValueX = 455;
        double marketValueX = 520;
        double headerY = 680;

        page2.AddText("Investment", 7, new PdfPoint(investmentX, headerY), boldFont);
        page2.AddText("Symbol", 7, new PdfPoint(symbolX, headerY), boldFont);
        page2.AddText("AccountNumber", 7, new PdfPoint(accountNumberX, headerY), boldFont);
        page2.AddText("Quantity", 7, new PdfPoint(quantityX, headerY), boldFont);
        page2.AddText("Price", 7, new PdfPoint(priceX, headerY), boldFont);
        page2.AddText("Book", 7, new PdfPoint(bookValueX, headerY), boldFont);
        page2.AddText("Market", 7, new PdfPoint(marketValueX, headerY), boldFont);

        // Second header row (Value labels)
        page2.AddText("MutualFund", 7, new PdfPoint(investmentX, headerY - 10), font);
        page2.AddText("Value", 7, new PdfPoint(bookValueX, headerY - 10), font);
        page2.AddText("Value", 7, new PdfPoint(marketValueX, headerY - 10), font);

        // Data rows
        double rowY = headerY - 30;
        foreach (
            var (name, symbol, accountNumber, quantity, price, bookValue, marketValue) in holdings
        )
        {
            page2.AddText(name, 7, new PdfPoint(investmentX, rowY), font);
            page2.AddText(symbol, 7, new PdfPoint(symbolX, rowY), font);
            page2.AddText(accountNumber, 7, new PdfPoint(accountNumberX, rowY), font);
            page2.AddText(quantity, 7, new PdfPoint(quantityX, rowY), font);
            page2.AddText(price, 7, new PdfPoint(priceX, rowY), font);
            page2.AddText(bookValue, 7, new PdfPoint(bookValueX, rowY), font);
            page2.AddText(marketValue, 7, new PdfPoint(marketValueX, rowY), font);
            rowY -= 12;

            if (includeDscAnnotation)
            {
                page2.AddText("DSC", 7, new PdfPoint(investmentX, rowY), font);
                rowY -= 12;
            }
        }

        // Total row
        if (totalBookValue is not null && totalMarketValue is not null)
        {
            rowY -= 5;
            page2.AddText("TotalPlanValue", 7, new PdfPoint(investmentX, rowY), boldFont);
            page2.AddText(totalBookValue, 7, new PdfPoint(bookValueX, rowY), boldFont);
            page2.AddText(totalMarketValue, 7, new PdfPoint(marketValueX, rowY), boldFont);
        }

        var ms = new MemoryStream();
        var pdfBytes = builder.Build();
        ms.Write(pdfBytes, 0, pdfBytes.Length);
        ms.Position = 0;
        return ms;
    }
}
