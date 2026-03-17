using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;

namespace Privestio.Application.Tests.Services;

public class SecurityResolutionServiceTests
{
    [Fact]
    public async Task ResolveOrCreateAsync_RepeatedNewSymbol_AddsSecurityOnce()
    {
        var repository = new Mock<ISecurityRepository>();
        repository
            .Setup(r => r.GetByAnySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Security?)null);
        repository
            .Setup(r => r.AddAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Security security, CancellationToken _) => security);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.Securities).Returns(repository.Object);

        var service = new SecurityResolutionService(unitOfWork.Object);

        var first = await service.ResolveOrCreateAsync("XEQT.TO", "XEQT", "CAD");
        var second = await service.ResolveOrCreateAsync("XEQT.TO", "XEQT", "CAD");

        first.Should().BeSameAs(second);
        repository.Verify(
            r => r.AddAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

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
