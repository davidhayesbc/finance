using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetAccountUncategorizedCounts;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetAccountUncategorizedCountsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock = new();

    public GetAccountUncategorizedCountsQueryTests()
    {
        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NoAccessibleAccounts_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();

        _accountRepositoryMock
            .Setup(x => x.GetAccessibleByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetAccountUncategorizedCountsQueryHandler(_unitOfWorkMock.Object);

        var result = await handler.Handle(
            new GetAccountUncategorizedCountsQuery(userId),
            CancellationToken.None
        );

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AccessibleAccounts_ReturnsPerAccountCountsIncludingZeros()
    {
        var userId = Guid.NewGuid();
        var accountA = CreateAccount(userId, "Joint Chequing");
        var accountB = CreateAccount(userId, "TFSA");

        _accountRepositoryMock
            .Setup(x => x.GetAccessibleByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([accountA, accountB]);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetUncategorizedCountsByAccountIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => ids.Contains(accountA.Id) && ids.Contains(accountB.Id)),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, int> { { accountA.Id, 7 } });

        var handler = new GetAccountUncategorizedCountsQueryHandler(_unitOfWorkMock.Object);

        var result = await handler.Handle(
            new GetAccountUncategorizedCountsQuery(userId),
            CancellationToken.None
        );

        result.Should().HaveCount(2);
        result.Single(r => r.AccountId == accountA.Id).UncategorizedCount.Should().Be(7);
        result.Single(r => r.AccountId == accountB.Id).UncategorizedCount.Should().Be(0);
    }

    private static Account CreateAccount(Guid ownerId, string name) =>
        new(
            name,
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ownerId
        );
}
