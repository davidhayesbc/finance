using Moq;
using Privestio.Application.Commands.CreateLot;
using Privestio.Application.Interfaces;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Commands;

public class CreateLotCommandTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesLot()
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
        var security = SecurityTestHelper.CreateSecurity(
            "XEQT.TO",
            "iShares Core Equity ETF Portfolio"
        );

        var holding = SecurityTestHelper.CreateHolding(
            account.Id,
            security,
            5m,
            new Money(40.25m, "CAD")
        );

        var uow = new Mock<IUnitOfWork>();
        var lots = new Mock<ILotRepository>();

        uow.Setup(x => x.Holdings.GetByIdAsync(holding.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(holding);
        uow.Setup(x => x.Accounts.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        uow.Setup(x => x.Lots).Returns(lots.Object);

        var handler = new CreateLotCommandHandler(uow.Object);

        var result = await handler.Handle(
            new CreateLotCommand(
                holding.Id,
                new DateOnly(2025, 2, 3),
                2.1361m,
                35.1098m,
                "CAD",
                userId,
                "Trade"
            ),
            CancellationToken.None
        );

        result.HoldingId.Should().Be(holding.Id);
        result.Quantity.Should().Be(2.1361m);
        result.UnitCost.Should().Be(35.1098m);
        lots.Verify(x => x.AddAsync(It.IsAny<Lot>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
