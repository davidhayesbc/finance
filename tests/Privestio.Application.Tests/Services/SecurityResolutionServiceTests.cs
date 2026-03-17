using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Application.Tests;

namespace Privestio.Application.Tests.Services;

public class SecurityResolutionServiceTests
{
    [Fact]
    public void GetPreferredPriceLookupSymbol_CadShareClass_FormatsYahooTicker()
    {
        var service = CreateService();
        var security = SecurityTestHelper.CreateSecurity("KILO.B", "Kilo Security", "CAD");

        var result = service.GetPreferredPriceLookupSymbol(security, "YahooFinance");

        result.Should().Be("KILO-B.TO");
    }

    [Fact]
    public void GetPreferredPriceLookupSymbol_CadFundSeries_FormatsYahooTicker()
    {
        var service = CreateService();
        var security = SecurityTestHelper.CreateSecurity("ZUAG.F", "Zuag Security", "CAD");

        var result = service.GetPreferredPriceLookupSymbol(security, "YahooFinance");

        result.Should().Be("ZUAG-F.TO");
    }

    [Fact]
    public void GetPreferredPriceLookupSymbol_WithExistingExchangeSuffix_LeavesTickerUntouched()
    {
        var service = CreateService();
        var security = SecurityTestHelper.CreateSecurity("CASH.TO", "Cash ETF", "CAD", true);

        var result = service.GetPreferredPriceLookupSymbol(security, "YahooFinance");

        result.Should().Be("CASH.TO");
    }

    private static SecurityResolutionService CreateService()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        SecurityTestHelper.CreateSecurityResolutionService(unitOfWork);
        return new SecurityResolutionService(unitOfWork.Object);
    }
}
