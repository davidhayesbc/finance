using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateTransfer;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class CreateTransferCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly CreateTransferCommandHandler _handler;
    private readonly List<Transaction> _addedTransactions = [];
    private readonly Guid _userId = Guid.NewGuid();

    public CreateTransferCommandTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.Setup(u => u.Transactions).Returns(_transactionRepo.Object);
        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepo.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _transactionRepo
            .Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => _addedTransactions.Add(t))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        _handler = new CreateTransferCommandHandler(_unitOfWork.Object);
    }

    private void SetupAccounts(Guid sourceId, Guid destId, Guid ownerId, string currency = "CAD")
    {
        var source = new Account("Source", AccountType.Banking, AccountSubType.Chequing, currency, new Money(0m, currency), DateOnly.FromDateTime(DateTime.UtcNow), ownerId);
        var dest = new Account("Dest", AccountType.Banking, AccountSubType.Chequing, currency, new Money(0m, currency), DateOnly.FromDateTime(DateTime.UtcNow), ownerId);

        _accountRepo.Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _accountRepo.Setup(r => r.GetByIdAsync(destId, It.IsAny<CancellationToken>())).ReturnsAsync(dest);
    }

    [Fact]
    public async Task Handle_CreatesLinkedDebitAndCreditTransactions()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        SetupAccounts(sourceId, destId, _userId);

        var command = new CreateTransferCommand(
            sourceId,
            destId,
            500.00m,
            "CAD",
            new DateTime(2025, 1, 15),
            _userId
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
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        SetupAccounts(sourceId, destId, _userId);

        var command = new CreateTransferCommand(
            sourceId,
            destId,
            100.00m,
            "CAD",
            DateTime.UtcNow,
            _userId
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
            DateTime.UtcNow,
            _userId
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
            DateTime.UtcNow,
            _userId
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
        SetupAccounts(sourceId, destId, _userId);

        var command = new CreateTransferCommand(sourceId, destId, 250.00m, "CAD", date, _userId);

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
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        SetupAccounts(sourceId, destId, _userId);

        var command = new CreateTransferCommand(
            sourceId,
            destId,
            100.00m,
            "CAD",
            DateTime.UtcNow,
            _userId
        );

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SourceNotOwned_ThrowsUnauthorizedAccessException()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupAccounts(sourceId, destId, otherUserId);

        var command = new CreateTransferCommand(
            sourceId,
            destId,
            100.00m,
            "CAD",
            DateTime.UtcNow,
            _userId
        );

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_CurrencyMismatch_ThrowsInvalidOperationException()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        SetupAccounts(sourceId, destId, _userId, "USD");

        var command = new CreateTransferCommand(
            sourceId,
            destId,
            100.00m,
            "CAD",
            DateTime.UtcNow,
            _userId
        );

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
