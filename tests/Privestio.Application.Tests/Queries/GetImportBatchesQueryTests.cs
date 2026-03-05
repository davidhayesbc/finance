using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetImportBatches;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class GetImportBatchesQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IImportBatchRepository> _importBatchRepo;
    private readonly GetImportBatchesQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public GetImportBatchesQueryTests()
    {
        _importBatchRepo = new Mock<IImportBatchRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.ImportBatches).Returns(_importBatchRepo.Object);
        _handler = new GetImportBatchesQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllBatchesForUser()
    {
        var batch1 = new ImportBatch("a.csv", "CSV", _userId);
        batch1.Complete(50, 45, 3, 2);
        var batch2 = new ImportBatch("b.ofx", "OFX", _userId);
        batch2.Complete(20, 20, 0, 0);

        _importBatchRepo
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([batch1, batch2]);

        var query = new GetImportBatchesQuery(_userId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].FileName.Should().Be("a.csv");
        result[1].FileName.Should().Be("b.ofx");
    }

    [Fact]
    public async Task Handle_NoBatches_ReturnsEmptyList()
    {
        _importBatchRepo
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new GetImportBatchesQuery(_userId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CalculatesMetricsForEachBatch()
    {
        var batch = new ImportBatch("test.csv", "CSV", _userId);
        batch.Complete(200, 180, 10, 10);

        _importBatchRepo
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([batch]);

        var query = new GetImportBatchesQuery(_userId);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].SuccessRate.Should().Be(0.90m);
        result[0].DuplicateRate.Should().Be(0.05m);
    }
}
