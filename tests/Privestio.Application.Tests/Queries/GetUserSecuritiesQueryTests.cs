using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetUserSecurities;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetUserSecuritiesQueryTests
{
    [Fact]
    public async Task Handle_UserHasSecurities_ReturnsCatalogWithLatestPriceMetadata()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0m),
            new DateOnly(2024, 1, 1),
            userId
        );

        var security = SecurityTestHelper.CreateSecurity(
            "XEQT.TO",
            "iShares Core Equity ETF Portfolio"
        );
        security.AddOrUpdateAlias("XEQT", "ImportTransactions", false, "XTSE");
        security.AddOrUpdateIdentifier(SecurityIdentifierType.Cusip, "46436D108", true);

        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            10m,
            new Money(30m, "CAD")
        );

        var latestPrice = SecurityTestHelper.CreatePriceHistory(
            security,
            35.25m,
            new DateOnly(2026, 3, 17),
            "YahooFinance",
            "XEQT.TO"
        );

        var accounts = new Mock<IAccountRepository>();
        var holdings = new Mock<IHoldingRepository>();
        var securities = new Mock<ISecurityRepository>();
        var prices = new Mock<IPriceHistoryRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        accounts
            .Setup(x => x.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);
        holdings
            .Setup(x => x.GetByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);
        securities
            .Setup(x =>
                x.GetByIdsAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([security]);
        prices
            .Setup(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)
                    new Dictionary<Guid, PriceHistory> { [security.Id] = latestPrice }
            );

        unitOfWork.Setup(x => x.Accounts).Returns(accounts.Object);
        unitOfWork.Setup(x => x.Holdings).Returns(holdings.Object);
        unitOfWork.Setup(x => x.Securities).Returns(securities.Object);
        unitOfWork.Setup(x => x.PriceHistories).Returns(prices.Object);

        var handler = new GetUserSecuritiesQueryHandler(unitOfWork.Object);

        var result = await handler.Handle(
            new GetUserSecuritiesQuery(userId),
            CancellationToken.None
        );

        result.Should().HaveCount(1);
        result[0].DisplaySymbol.Should().Be("XEQT.TO");
        result[0].Currency.Should().Be("CAD");
        result[0].LatestPriceSource.Should().Be("YahooFinance");
        result[0].LatestPrice.Should().Be(35.25m);
        result[0].Identifiers.Should().Contain(i => i.IdentifierType == "Cusip");
    }
}
