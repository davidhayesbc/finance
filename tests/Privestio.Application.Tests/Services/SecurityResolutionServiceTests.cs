using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

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

        var service = new SecurityResolutionService(
            unitOfWork.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<SecurityResolutionService>>()
        );

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

    [Fact]
    public async Task ResolveOrCreateAsync_WithMatchingCusip_ResolvesExistingSecurity()
    {
        var existing = SecurityTestHelper.CreateSecurity(
            "XEQT",
            "iShares Core Equity ETF Portfolio"
        );
        existing.AddOrUpdateIdentifier(SecurityIdentifierType.Cusip, "46436D108", true);

        var unitOfWork = new Mock<IUnitOfWork>();
        var service = SecurityTestHelper.CreateSecurityResolutionService(unitOfWork, [existing]);

        var resolved = await service.ResolveOrCreateAsync(
            "XEQT.TO",
            "iShares Core Equity ETF Portfolio",
            "CAD",
            source: "Wealthsimple",
            exchange: "XTSE",
            identifiers: new Dictionary<SecurityIdentifierType, string>
            {
                [SecurityIdentifierType.Cusip] = "46436D108",
            }
        );

        resolved.Id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task ResolveOrCreateAsync_WithSourceAndExchange_PrefersExactAliasContext()
    {
        var xeqtTsx = SecurityTestHelper.CreateSecurity("XEQT", "XEQT TSX");
        xeqtTsx.AddOrUpdateAlias("XEQT", "ProviderA", true, "XTSE");

        var xeqtOther = SecurityTestHelper.CreateSecurity("XEQT", "XEQT Other", currency: "USD");
        xeqtOther.AddOrUpdateAlias("XEQT", "ProviderA", true, "XNAS");

        var unitOfWork = new Mock<IUnitOfWork>();
        var service = SecurityTestHelper.CreateSecurityResolutionService(
            unitOfWork,
            [xeqtTsx, xeqtOther]
        );

        var resolved = await service.ResolveOrCreateAsync(
            "XEQT",
            "XEQT",
            "CAD",
            source: "ProviderA",
            exchange: "XTSE"
        );

        resolved.Id.Should().Be(xeqtTsx.Id);
    }

    private static SecurityResolutionService CreateService()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        SecurityTestHelper.CreateSecurityResolutionService(unitOfWork);
        return new SecurityResolutionService(
            unitOfWork.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<SecurityResolutionService>>()
        );
    }

    [Fact]
    public async Task ResolveOrCreateAsync_NewSecurity_CreatesSourceAlias()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = SecurityTestHelper.CreateSecurityResolutionService(unitOfWork);

        var security = await service.ResolveOrCreateAsync(
            "ZFL",
            "BMO Long Federal Bond Index ETF",
            "CAD",
            source: "Wealthsimple"
        );

        security.HasAlias("ZFL", "Wealthsimple").Should().BeTrue();
    }

    [Fact]
    public async Task ResolveOrCreateAsync_NewSecurity_CreatesYahooFinanceAlias()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = SecurityTestHelper.CreateSecurityResolutionService(unitOfWork);

        var security = await service.ResolveOrCreateAsync(
            "ZFL",
            "BMO Long Federal Bond Index ETF",
            "CAD",
            source: "Wealthsimple"
        );

        security.HasAlias("ZFL.TO", "YahooFinance").Should().BeTrue();
    }

    [Fact]
    public async Task ResolveOrCreateAsync_NewUsdSecurity_CreatesYahooFinanceAliasWithoutToSuffix()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = SecurityTestHelper.CreateSecurityResolutionService(unitOfWork);

        var security = await service.ResolveOrCreateAsync(
            "VTI",
            "Vanguard Total Stock Market ETF",
            "USD",
            source: "Wealthsimple"
        );

        security.HasAlias("VTI", "YahooFinance").Should().BeTrue();
    }

    [Fact]
    public async Task ResolveOrCreateAsync_ExistingSecurity_AddsSourceAliasEvenWhenSymbolMatchesDisplay()
    {
        var existing = SecurityTestHelper.CreateSecurity("ZCS", "BMO Short Corp Bond", "CAD");
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = SecurityTestHelper.CreateSecurityResolutionService(unitOfWork, [existing]);

        var resolved = await service.ResolveOrCreateAsync(
            "ZCS",
            "BMO Short Corporate Bond Index ETF",
            "CAD",
            source: "WorldSource"
        );

        resolved.HasAlias("ZCS", "WorldSource").Should().BeTrue();
    }

    [Fact]
    public async Task ResolveOrCreateAsync_ExistingSecurity_AddsYahooFinanceAliasIfMissing()
    {
        var existing = SecurityTestHelper.CreateSecurity("ZCS", "BMO Short Corp Bond", "CAD");
        var unitOfWork = new Mock<IUnitOfWork>();
        var service = SecurityTestHelper.CreateSecurityResolutionService(unitOfWork, [existing]);

        await service.ResolveOrCreateAsync(
            "ZCS",
            "BMO Short Corporate Bond Index ETF",
            "CAD",
            source: "WorldSource"
        );

        existing.HasAlias("ZCS.TO", "YahooFinance").Should().BeTrue();
    }
}
