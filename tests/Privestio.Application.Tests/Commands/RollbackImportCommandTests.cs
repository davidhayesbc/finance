using FluentAssertions;
using Moq;
using Privestio.Application.Commands.RollbackImport;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class RollbackImportCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IImportBatchRepository> _importBatchRepo;
    private readonly RollbackImportCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public RollbackImportCommandTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _importBatchRepo = new Mock<IImportBatchRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.Setup(u => u.Transactions).Returns(_transactionRepo.Object);
        _unitOfWork.Setup(u => u.ImportBatches).Returns(_importBatchRepo.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _importBatchRepo
            .Setup(r => r.UpdateAsync(It.IsAny<ImportBatch>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch b, CancellationToken _) => b);

        _handler = new RollbackImportCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidBatch_SoftDeletesTransactionsAndSetsRolledBack()
    {
        var batch = new ImportBatch("test.csv", "CSV", _userId);
        batch.Complete(3, 3, 0, 0);

        var transactions = new List<Transaction>
        {
            new(Guid.NewGuid(), DateTime.UtcNow, new Money(-10m), "Txn1", TransactionType.Debit),
            new(Guid.NewGuid(), DateTime.UtcNow, new Money(-20m), "Txn2", TransactionType.Debit),
        };

        _importBatchRepo
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);
        _transactionRepo
            .Setup(r => r.GetByImportBatchIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await _handler.Handle(
            new RollbackImportCommand(batch.Id, _userId),
            CancellationToken.None
        );

        result.Should().BeTrue();
        transactions.Should().OnlyContain(t => t.IsDeleted);
        batch.Status.Should().Be(ImportStatus.RolledBack);
    }

    [Fact]
    public async Task Handle_BatchNotFound_ReturnsFalse()
    {
        _importBatchRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch?)null);

        var result = await _handler.Handle(
            new RollbackImportCommand(Guid.NewGuid(), _userId),
            CancellationToken.None
        );

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WrongUser_ReturnsFalse()
    {
        var batch = new ImportBatch("test.csv", "CSV", _userId);
        batch.Complete(3, 3, 0, 0);

        _importBatchRepo
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var result = await _handler.Handle(
            new RollbackImportCommand(batch.Id, Guid.NewGuid()), // different user
            CancellationToken.None
        );

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AlreadyRolledBack_ReturnsFalse()
    {
        var batch = new ImportBatch("test.csv", "CSV", _userId);
        batch.Status = ImportStatus.RolledBack;

        _importBatchRepo
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var result = await _handler.Handle(
            new RollbackImportCommand(batch.Id, _userId),
            CancellationToken.None
        );

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        var batch = new ImportBatch("test.csv", "CSV", _userId);
        batch.Complete(1, 1, 0, 0);

        _importBatchRepo
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);
        _transactionRepo
            .Setup(r => r.GetByImportBatchIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        await _handler.Handle(new RollbackImportCommand(batch.Id, _userId), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
