using Moq;
using Privestio.Application.Commands.DeleteSecurityAlias;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Commands;

public class DeleteSecurityAliasCommandTests
{
    [Fact]
    public async Task Handle_ValidOwnedSecurity_RemovesAlias()
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

        var security = SecurityTestHelper.CreateSecurity("VFV.TO", "Vanguard ETF");
        var alias = security.AddOrUpdateAlias("VFV.TO", "YahooFinance", false, "XTSE");
        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            3m,
            new Money(100m, "CAD")
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

        var handler = new DeleteSecurityAliasCommandHandler(unitOfWork.Object);

        var deleted = await handler.Handle(
            new DeleteSecurityAliasCommand(security.Id, alias.Id, userId),
            CancellationToken.None
        );

        deleted.Should().BeTrue();
        security.Aliases.Should().NotContain(a => a.Id == alias.Id);
    }
}
