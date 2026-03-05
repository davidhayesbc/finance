using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetImportBatch;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class GetImportBatchQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IImportBatchRepository> _importBatchRepo;
    private readonly GetImportBatchQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetImportBatchQueryTests()
    {
        _importBatchRepo = new Mock<IImportBatchRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.ImportBatches).Returns(_importBatchRepo.Object);
        _handler = new GetImportBatchQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingBatch_ReturnsBatchWithMetrics()
    {
        var batch = new ImportBatch("test.csv", "CSV", _userId);
        batch.Complete(100, 85, 5, 10);

        _importBatchRepo
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var query = new GetImportBatchQuery(batch.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.RowCount.Should().Be(100);
        result.SuccessCount.Should().Be(85);
        result.ErrorCount.Should().Be(5);
        result.DuplicateCount.Should().Be(10);
        result.SuccessRate.Should().Be(0.85m);
        result.DuplicateRate.Should().Be(0.10m);
    }

    [Fact]
    public async Task Handle_BatchNotFound_ReturnsNull()
    {
        _importBatchRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch?)null);

        var query = new GetImportBatchQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ZeroRowBatch_AvoidsDivideByZero()
    {
        var batch = new ImportBatch("empty.csv", "CSV", _userId);
        batch.Complete(0, 0, 0, 0);

        _importBatchRepo
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var query = new GetImportBatchQuery(batch.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.SuccessRate.Should().Be(0m);
        result.DuplicateRate.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_BatchWithErrors_IncludesErrorDetails()
    {
        var batch = new ImportBatch("test.csv", "CSV", _userId);
        batch.Complete(10, 8, 2, 0);
        batch.ErrorDetails = """[{"RowNumber":3,"Message":"Invalid date","RawData":"bad,row"}]""";

        _importBatchRepo
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var query = new GetImportBatchQuery(batch.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Errors.Should().HaveCount(1);
        result.Errors[0].RowNumber.Should().Be(3);
        result.Errors[0].Message.Should().Be("Invalid date");
    }
}
