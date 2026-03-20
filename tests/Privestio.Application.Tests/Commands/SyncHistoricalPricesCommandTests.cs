using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Privestio.Application.Commands.SyncHistoricalPrices;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class SyncHistoricalPricesCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IHoldingRepository> _holdings = new();
    private readonly Mock<IPriceHistoryRepository> _priceHistories = new();
    private readonly Mock<IPriceFeedProvider> _priceFeedProvider = new();

    public SyncHistoricalPricesCommandTests()
    {
        _unitOfWork.SetupGet(x => x.Accounts).Returns(_accounts.Object);
        _unitOfWork.SetupGet(x => x.Holdings).Returns(_holdings.Object);
        _unitOfWork.SetupGet(x => x.PriceHistories).Returns(_priceHistories.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _priceFeedProvider.SetupGet(x => x.ProviderName).Returns("YahooFinance");

        _priceHistories
            .Setup(x =>
                x.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<(Guid SecurityId, DateOnly AsOfDate)>());
    }

    [Fact]
    public async Task Handle_NoInvestmentAccounts_ReturnsEmptyResult()
    {
        var userId = Guid.NewGuid();
        var nonInvestment = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(100m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            userId
        );

        _accounts
            .Setup(x => x.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([nonInvestment]);

        var handler = new SyncHistoricalPricesCommandHandler(
            _unitOfWork.Object,
            _priceFeedProvider.Object,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWork),
            Options.Create(new PricingOptions())
        );

        var result = await handler.Handle(
            new SyncHistoricalPricesCommand(
                userId,
                new DateOnly(2024, 1, 1),
                new DateOnly(2024, 1, 31)
            ),
            CancellationToken.None
        );

        result.Provider.Should().Be("YahooFinance");
        result.SecuritiesProcessed.Should().Be(0);
        result.QuotesFetched.Should().Be(0);
        result.QuotesInserted.Should().Be(0);
        _priceFeedProvider.Verify(
            x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithHistoricalQuotes_PersistsOnlyNewEntries()
    {
        var userId = Guid.NewGuid();
        var investment = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(0m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            userId
        );

        var security = SecurityTestHelper.CreateSecurity(
            "VFV",
            "Vanguard S&P 500",
            "CAD",
            false,
            ("VFV.TO", "YahooFinance", true)
        );
        var holding = SecurityTestHelper.CreateHolding(
            investment.Id,
            security,
            10m,
            new Money(100m)
        );

        _accounts
            .Setup(x => x.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([investment]);
        _holdings
            .Setup(x => x.GetByAccountIdAsync(investment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);

        _priceFeedProvider
            .Setup(x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    new DateOnly(2024, 1, 1),
                    new DateOnly(2024, 1, 31),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([
                new PriceQuote(security.Id, "VFV.TO", 105.25m, "CAD", new DateOnly(2024, 1, 2)),
                new PriceQuote(security.Id, "VFV.TO", 106.10m, "CAD", new DateOnly(2024, 1, 3)),
            ]);

        _priceHistories
            .Setup(x =>
                x.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new HashSet<(Guid SecurityId, DateOnly AsOfDate)>
                {
                    (security.Id, new DateOnly(2024, 1, 2)),
                }
            );

        var handler = new SyncHistoricalPricesCommandHandler(
            _unitOfWork.Object,
            _priceFeedProvider.Object,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWork, [security]),
            Options.Create(new PricingOptions())
        );

        var result = await handler.Handle(
            new SyncHistoricalPricesCommand(
                userId,
                new DateOnly(2024, 1, 1),
                new DateOnly(2024, 1, 31)
            ),
            CancellationToken.None
        );

        result.SecuritiesProcessed.Should().Be(1);
        result.QuotesFetched.Should().Be(2);
        result.QuotesInserted.Should().Be(1);
        result.QuotesSkipped.Should().Be(1);

        _priceHistories.Verify(
            x =>
                x.AddRangeAsync(
                    It.Is<IEnumerable<PriceHistory>>(entries => entries.Count() == 1),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
