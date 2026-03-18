using Moq;
using Privestio.Application.Commands.UpdateSecurityAlias;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Commands;

public class UpdateSecurityAliasCommandTests
{
    [Fact]
    public async Task Handle_ValidOwnedSecurity_UpdatesAliasById()
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

        var security = SecurityTestHelper.CreateSecurity("CASH.TO", "Cash ETF");
        var alias = security.AddOrUpdateAlias("CASH", "Wealthsimple", false, "XTSE");

        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            1m,
            new Money(50m, "CAD")
        );

        var accounts = new Mock<IAccountRepository>();
        var holdings = new Mock<IHoldingRepository>();
        var securities = new Mock<ISecurityRepository>();
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

        unitOfWork.Setup(x => x.Accounts).Returns(accounts.Object);
        unitOfWork.Setup(x => x.Holdings).Returns(holdings.Object);
        unitOfWork.Setup(x => x.Securities).Returns(securities.Object);
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateSecurityAliasCommandHandler(unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateSecurityAliasCommand(
                security.Id,
                alias.Id,
                "CASH-WS",
                "Wealthsimple",
                "XTSX",
                true,
                userId
            ),
            CancellationToken.None
        );

        result.Id.Should().Be(alias.Id);
        result.SecurityId.Should().Be(security.Id);
        result.Symbol.Should().Be("CASH-WS");
        result.Source.Should().Be("Wealthsimple");
        result.Exchange.Should().Be("XTSX");
        result.IsPrimary.Should().BeTrue();
    }
}
