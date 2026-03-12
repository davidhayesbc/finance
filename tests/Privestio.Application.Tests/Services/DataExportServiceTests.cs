using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Services;

public class DataExportServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly DataExportService _service;

    public DataExportServiceTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(_transactionRepoMock.Object);
        _service = new DataExportService();
    }

    [Fact]
    public async Task ExportUserData_WithAccounts_ReturnsJsonWithAccounts()
    {
        var userId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new(
                "Checking",
                AccountType.Banking,
                AccountSubType.Chequing,
                "CAD",
                new Money(0m),
                DateOnly.FromDateTime(DateTime.UtcNow),
                userId
            ),
        };
        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction>());

        var result = await _service.ExportUserDataAsync(_unitOfWorkMock.Object, userId);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Checking");
    }

    [Fact]
    public async Task ExportUserData_NoData_ReturnsEmptyJsonStructure()
    {
        var userId = Guid.NewGuid();
        _accountRepoMock
            .Setup(r => r.GetByOwnerIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
        _transactionRepoMock
            .Setup(r =>
                r.GetByOwnerAndDateRangeAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<Transaction>());

        var result = await _service.ExportUserDataAsync(_unitOfWorkMock.Object, userId);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("accounts");
    }
}
