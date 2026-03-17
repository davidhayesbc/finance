using Moq;
using Privestio.Application.Commands.CreateHolding;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Commands;

public class CreateHoldingCommandTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesHolding()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Money(0m),
            new DateOnly(2025, 1, 1),
            userId
        );

        var uow = new Mock<IUnitOfWork>();
        var holdings = new Mock<IHoldingRepository>();
        var securityResolutionService = SecurityTestHelper.CreateSecurityResolutionService(uow);

        uow.Setup(x => x.Accounts.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        uow.Setup(x => x.Holdings).Returns(holdings.Object);
        holdings
            .Setup(x =>
                x.GetByAccountIdAndSecurityIdAsync(
                    account.Id,
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Holding?)null);

        var handler = new CreateHoldingCommandHandler(uow.Object, securityResolutionService);

        var result = await handler.Handle(
            new CreateHoldingCommand(
                account.Id,
                "XEQT.TO",
                "iShares Core Equity ETF Portfolio",
                5m,
                40.25m,
                "CAD",
                userId
            ),
            CancellationToken.None
        );

        result.AccountId.Should().Be(account.Id);
        result.Symbol.Should().Be("XEQT.TO");
        result.Quantity.Should().Be(5m);
        result.SecurityId.Should().NotBe(Guid.Empty);
        holdings.Verify(
            x => x.AddAsync(It.IsAny<Holding>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
