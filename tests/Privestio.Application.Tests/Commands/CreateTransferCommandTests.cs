using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateTransfer;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class CreateTransferCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly CreateTransferCommandHandler _handler;
    private readonly List<Transaction> _addedTransactions = [];

    public CreateTransferCommandTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.Setup(u => u.Transactions).Returns(_transactionRepo.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => _addedTransactions.Add(t))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        _handler = new CreateTransferCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_CreatesLinkedDebitAndCreditTransactions()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var command = new CreateTransferCommand(
            sourceId,
            destId,
            500.00m,
            "CAD",
            new DateTime(2025, 1, 15)
        );

        var result = await _handler.Handle(command, CancellationToken.None);

        _addedTransactions.Should().HaveCount(2);

        var source = _addedTransactions.First(t => t.AccountId == sourceId);
        var dest = _addedTransactions.First(t => t.AccountId == destId);

        source.Amount.Amount.Should().Be(-500.00m);
        source.Type.Should().Be(TransactionType.Transfer);
        dest.Amount.Amount.Should().Be(500.00m);
        dest.Type.Should().Be(TransactionType.Transfer);
    }

    [Fact]
    public async Task Handle_LinksTransactionsBidirectionally()
    {
        var command = new CreateTransferCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100.00m,
            "CAD",
            DateTime.UtcNow
        );

        await _handler.Handle(command, CancellationToken.None);

        var source = _addedTransactions[0];
        var dest = _addedTransactions[1];

        source.LinkedTransferId.Should().Be(dest.Id);
        dest.LinkedTransferId.Should().Be(source.Id);
    }

    [Fact]
    public async Task Handle_SameAccount_ThrowsArgumentException()
    {
        var accountId = Guid.NewGuid();
        var command = new CreateTransferCommand(
            accountId,
            accountId,
            100.00m,
            "CAD",
            DateTime.UtcNow
        );

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        var command = new CreateTransferCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            -100.00m,
            "CAD",
            DateTime.UtcNow
        );

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Handle_ReturnsCorrectResponse()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var date = new DateTime(2025, 1, 15);

        var command = new CreateTransferCommand(sourceId, destId, 250.00m, "CAD", date);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Amount.Should().Be(250.00m);
        result.Currency.Should().Be("CAD");
        result.Date.Should().Be(date);
        result.SourceTransactionId.Should().NotBeEmpty();
        result.DestinationTransactionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        var command = new CreateTransferCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100.00m,
            "CAD",
            DateTime.UtcNow
        );

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
