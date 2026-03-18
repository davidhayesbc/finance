using Moq;
using Privestio.Application.Commands.UpdateSecurityDetails;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Commands;

public class UpdateSecurityDetailsCommandTests
{
    [Fact]
    public async Task Handle_ValidOwnedSecurity_UpdatesAndReturnsCatalogItem()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(0m),
            new DateOnly(2024, 1, 1),
            userId
        );

        var security = SecurityTestHelper.CreateSecurity("VFV.TO", "Vanguard S&P 500 Index ETF");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            5m,
            new Money(100m, "CAD")
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
            .Setup(x => x.GetByIdAsync(security.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(security);
        securities
            .Setup(x => x.UpdateAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Security s, CancellationToken _) => s);
        prices
            .Setup(x =>
                x.GetLatestBySecurityIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                (IReadOnlyDictionary<Guid, PriceHistory>)new Dictionary<Guid, PriceHistory>()
            );

        unitOfWork.Setup(x => x.Accounts).Returns(accounts.Object);
        unitOfWork.Setup(x => x.Holdings).Returns(holdings.Object);
        unitOfWork.Setup(x => x.Securities).Returns(securities.Object);
        unitOfWork.Setup(x => x.PriceHistories).Returns(prices.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateSecurityDetailsCommandHandler(unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateSecurityDetailsCommand(
                security.Id,
                "Vanguard S&P 500 ETF",
                "VFV",
                "USD",
                "XNAS",
                true,
                userId
            ),
            CancellationToken.None
        );

        result.DisplaySymbol.Should().Be("VFV");
        result.Name.Should().Be("Vanguard S&P 500 ETF");
        result.Currency.Should().Be("USD");
        result.Exchange.Should().Be("XNAS");
        result.IsCashEquivalent.Should().BeTrue();

        securities.Verify(
            x => x.UpdateAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
