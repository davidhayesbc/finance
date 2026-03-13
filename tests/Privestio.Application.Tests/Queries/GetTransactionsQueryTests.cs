using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetTransactions;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetTransactionsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;

    public GetTransactionsQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();

        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumUpToAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(0m);
    }

    [Fact]
    public async Task Handle_AccountNotOwned_ReturnsEmptyPage()
    {
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var account = CreateAccount(otherOwnerId, openingBalance: 1000m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GetTransactionsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetTransactionsQuery(accountId, ownerId, 20, SearchTerm: "rent");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.PageSize.Should().Be(20);
        _transactionRepositoryMock.Verify(
            x =>
                x.GetPagedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<DateRange?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_DefaultPage_ComputesFromUpToSum_AndEmitsCursorAnchor()
    {
        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(ownerId, openingBalance: 100m);

        // Newest -> oldest
        var t3 = CreateTransaction(
            accountId,
            new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            200m,
            TransactionType.Credit
        );
        var t2 = CreateTransaction(
            accountId,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            40m,
            TransactionType.Debit
        );
        var t1 = CreateTransaction(
            accountId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            100m,
            TransactionType.Credit
        );

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetPagedAsync(
                    accountId,
                    15,
                    null,
                    It.IsAny<DateRange?>(),
                    null,
                    null,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((new List<Transaction> { t3, t2, t1 }, $"{t1.Date:O}|{t1.Id}"));

        // Signed sum up to newest row t3 = +100 -40 +200 = +260
        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumUpToAsync(accountId, t3.Date, t3.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(260m);

        var handler = new GetTransactionsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetTransactionsQuery(accountId, ownerId, 15);

        var result = await handler.Handle(query, CancellationToken.None);

        // Running: t3=360, t2=160, t1=200 with opening=100
        result.Items.Should().HaveCount(3);
        result.Items[0].RunningBalance.Should().Be(360m);
        result.Items[1].RunningBalance.Should().Be(160m);
        result.Items[2].RunningBalance.Should().Be(200m);
        // Anchor stores balance before oldest row: 200 - (+100) = 100
        result.NextCursor.Should().Be($"{t1.Date:O}|{t1.Id}|100");
    }

    [Fact]
    public async Task Handle_SubsequentPage_UsesCursorAnchor_WithoutExtraAggregate()
    {
        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(ownerId, openingBalance: 100m);

        var t2 = CreateTransaction(
            accountId,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            40m,
            TransactionType.Debit
        );
        var t1 = CreateTransaction(
            accountId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            100m,
            TransactionType.Credit
        );

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetPagedAsync(
                    accountId,
                    15,
                    "2026-01-03T00:00:00.0000000Z|00000000-0000-0000-0000-000000000001|360",
                    It.IsAny<DateRange?>(),
                    null,
                    null,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((new List<Transaction> { t2, t1 }, (string?)null));

        var handler = new GetTransactionsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetTransactionsQuery(
            accountId,
            ownerId,
            15,
            "2026-01-03T00:00:00.0000000Z|00000000-0000-0000-0000-000000000001|360"
        );

        var result = await handler.Handle(query, CancellationToken.None);

        // From anchor 360: t2=360, t1=400
        result.Items.Should().HaveCount(2);
        result.Items[0].RunningBalance.Should().Be(360m);
        result.Items[1].RunningBalance.Should().Be(400m);

        _transactionRepositoryMock.Verify(
            x =>
                x.GetSignedSumUpToAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_SearchFilter_ComputesExactPerRowRunningBalances()
    {
        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(ownerId, openingBalance: 0m);

        // Sparse selection (search/category can skip rows)
        var t3 = CreateTransaction(
            accountId,
            new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            200m,
            TransactionType.Credit
        );
        var t1 = CreateTransaction(
            accountId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            100m,
            TransactionType.Credit
        );

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetPagedAsync(
                    accountId,
                    15,
                    null,
                    It.IsAny<DateRange?>(),
                    null,
                    "coffee",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((new List<Transaction> { t3, t1 }, (string?)null));

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumUpToAsync(accountId, t3.Date, t3.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(260m);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumUpToAsync(accountId, t1.Date, t1.Id, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(100m);

        var handler = new GetTransactionsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetTransactionsQuery(accountId, ownerId, 15, SearchTerm: "coffee");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].RunningBalance.Should().Be(260m);
        result.Items[1].RunningBalance.Should().Be(100m);

        _transactionRepositoryMock.Verify(
            x =>
                x.GetSignedSumUpToAsync(
                    accountId,
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Handle_EmptyPage_ReturnsEmptyWithoutAggregateCalls()
    {
        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateAccount(ownerId, openingBalance: 0m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x =>
                x.GetPagedAsync(
                    accountId,
                    It.IsAny<int>(),
                    null,
                    It.IsAny<DateRange?>(),
                    null,
                    null,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((new List<Transaction>(), (string?)null));

        var handler = new GetTransactionsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetTransactionsQuery(accountId, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        _transactionRepositoryMock.Verify(
            x =>
                x.GetSignedSumUpToAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    private static Account CreateAccount(Guid ownerId, decimal openingBalance)
    {
        return new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2025, 1, 1),
            ownerId
        );
    }

    private static Transaction CreateTransaction(
        Guid accountId,
        DateTime date,
        decimal amount,
        TransactionType type
    ) => new(accountId, date, new Money(amount), $"{type} {amount}", type);
}
