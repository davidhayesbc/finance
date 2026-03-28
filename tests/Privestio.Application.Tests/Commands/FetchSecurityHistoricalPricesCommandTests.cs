using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Privestio.Application.Commands.FetchSecurityHistoricalPrices;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Interfaces;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class FetchSecurityHistoricalPricesCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILotRepository> _lots = new();
    private readonly Mock<IPriceHistoryRepository> _priceHistories = new();
    private readonly Mock<IPriceFeedProvider> _priceFeedProvider = new();

    private readonly Guid _userId = Guid.NewGuid();

    public FetchSecurityHistoricalPricesCommandTests()
    {
        _unitOfWork.Setup(x => x.Lots).Returns(_lots.Object);
        _unitOfWork.Setup(x => x.PriceHistories).Returns(_priceHistories.Object);
        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

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

    /// <summary>
    /// Creates the handler with <paramref name="securities"/> registered in the security
    /// resolution service. Securities are also findable via IUnitOfWork.Securities.GetByIdAsync.
    /// </summary>
    private FetchSecurityHistoricalPricesCommandHandler CreateHandler(
        params Security[] securities
    ) =>
        new(
            _unitOfWork.Object,
            _priceFeedProvider.Object,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWork, securities),
            Options.Create(new PricingOptions())
        );

    [Fact]
    public async Task Handle_SecurityNotFound_ThrowsKeyNotFoundException()
    {
        var handler = CreateHandler(); // no securities registered

        var act = () =>
            handler.Handle(
                new FetchSecurityHistoricalPricesCommand(Guid.NewGuid(), _userId),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NoFromDate_UsesEarliestLotAcquiredDate()
    {
        var security = SecurityTestHelper.CreateSecurity("XEQT", "iShares Core Equity ETF");
        var earliest = new DateOnly(2022, 3, 15);

        _lots
            .Setup(x =>
                x.GetEarliestAcquiredDateBySecurityIdAsync(
                    security.Id,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(earliest);

        _priceFeedProvider
            .Setup(x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var handler = CreateHandler(security);
        var result = await handler.Handle(
            new FetchSecurityHistoricalPricesCommand(security.Id, _userId),
            CancellationToken.None
        );

        result.FromDate.Should().Be(earliest);
        _priceFeedProvider.Verify(
            x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    earliest,
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_NoFromDate_NoLots_FallsBackToTenYearLookback()
    {
        var security = SecurityTestHelper.CreateSecurity("VFV", "Vanguard S&P 500");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _lots
            .Setup(x =>
                x.GetEarliestAcquiredDateBySecurityIdAsync(
                    security.Id,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((DateOnly?)null);

        _priceFeedProvider
            .Setup(x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var handler = CreateHandler(security);
        var result = await handler.Handle(
            new FetchSecurityHistoricalPricesCommand(security.Id, _userId),
            CancellationToken.None
        );

        result.FromDate.Should().BeBefore(today.AddYears(-9));
    }

    [Fact]
    public async Task Handle_ExplicitFromDate_UsesProvidedDate()
    {
        var security = SecurityTestHelper.CreateSecurity("XEQT", "iShares Core Equity ETF");
        var fromDate = new DateOnly(2023, 1, 1);
        var toDate = new DateOnly(2023, 12, 31);

        _priceFeedProvider
            .Setup(x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var handler = CreateHandler(security);
        var result = await handler.Handle(
            new FetchSecurityHistoricalPricesCommand(security.Id, _userId, fromDate, toDate),
            CancellationToken.None
        );

        result.FromDate.Should().Be(fromDate);
        result.ToDate.Should().Be(toDate);
        _lots.Verify(
            x =>
                x.GetEarliestAcquiredDateBySecurityIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ProviderReturnsQuotes_PersistsNewEntries()
    {
        var security = SecurityTestHelper.CreateSecurity("XEQT", "iShares Core Equity ETF");
        var fromDate = new DateOnly(2024, 1, 1);
        var toDate = new DateOnly(2024, 1, 3);

        var quotes = new List<PriceQuote>
        {
            new(security.Id, "XEQT.TO", 35.00m, "CAD", new DateOnly(2024, 1, 2), "YahooFinance"),
            new(security.Id, "XEQT.TO", 35.50m, "CAD", new DateOnly(2024, 1, 3), "YahooFinance"),
        };

        _priceFeedProvider
            .Setup(x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    fromDate,
                    toDate,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(quotes);

        var handler = CreateHandler(security);
        var result = await handler.Handle(
            new FetchSecurityHistoricalPricesCommand(security.Id, _userId, fromDate, toDate),
            CancellationToken.None
        );

        result.QuotesFetched.Should().Be(2);
        result.QuotesInserted.Should().Be(2);
        result.QuotesSkipped.Should().Be(0);
        result.SecuritiesProcessed.Should().Be(1);

        _priceHistories.Verify(
            x =>
                x.AddRangeAsync(
                    It.Is<IEnumerable<PriceHistory>>(entries => entries.Count() == 2),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_AllQuotesAlreadyExist_SkipsAllAndDoesNotSave()
    {
        var security = SecurityTestHelper.CreateSecurity("XEQT", "iShares Core Equity ETF");
        var fromDate = new DateOnly(2024, 1, 1);
        var toDate = new DateOnly(2024, 1, 2);

        _priceFeedProvider
            .Setup(x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                [new(security.Id, "XEQT.TO", 35.00m, "CAD", new DateOnly(2024, 1, 2), "YahooFinance")]
            );

        _priceHistories
            .Setup(x =>
                x.GetExistingKeysAsync(
                    It.IsAny<IEnumerable<(Guid SecurityId, DateOnly AsOfDate)>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new HashSet<(Guid, DateOnly)> { (security.Id, new DateOnly(2024, 1, 2)) }
            );

        var handler = CreateHandler(security);
        var result = await handler.Handle(
            new FetchSecurityHistoricalPricesCommand(security.Id, _userId, fromDate, toDate),
            CancellationToken.None
        );

        result.QuotesInserted.Should().Be(0);
        result.QuotesSkipped.Should().Be(1);

        _priceHistories.Verify(
            x =>
                x.AddRangeAsync(
                    It.IsAny<IEnumerable<PriceHistory>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ProviderReturnsNoQuotes_ReturnsZeroCounts()
    {
        var security = SecurityTestHelper.CreateSecurity("XEQT", "iShares Core Equity ETF");

        _lots
            .Setup(x =>
                x.GetEarliestAcquiredDateBySecurityIdAsync(
                    security.Id,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new DateOnly(2023, 6, 1));

        _priceFeedProvider
            .Setup(x =>
                x.GetHistoricalPricesAsync(
                    It.IsAny<PriceLookup>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        var handler = CreateHandler(security);
        var result = await handler.Handle(
            new FetchSecurityHistoricalPricesCommand(security.Id, _userId),
            CancellationToken.None
        );

        result.QuotesFetched.Should().Be(0);
        result.QuotesInserted.Should().Be(0);
        result.SecuritiesProcessed.Should().Be(1);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
