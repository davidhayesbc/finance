using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetAccounts;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetAccountsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;

    public GetAccountsQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();

        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NoAccounts_ReturnsEmptyList()
    {
        var ownerId = Guid.NewGuid();

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal>());

        var handler = new GetAccountsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountsQuery(ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MixedAccounts_ComputesCorrectBalances()
    {
        var ownerId = Guid.NewGuid();
        var banking = CreateAccount(
            ownerId,
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            500m
        );
        var credit = CreateAccount(
            ownerId,
            "Visa",
            AccountType.Credit,
            AccountSubType.CreditCard,
            0m
        );
        var property = CreateAccount(
            ownerId,
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            770_000m
        );
        AddValuation(property, 850_000m, new DateOnly(2026, 1, 1));

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { banking, credit, property });

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new Dictionary<Guid, decimal> { { banking.Id, 250m }, { credit.Id, -1200m } }
            );

        var handler = new GetAccountsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountsQuery(ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(3);

        var bankingResult = result.Single(a => a.Name == "Chequing");
        bankingResult.CurrentBalance.Should().Be(750m);

        var creditResult = result.Single(a => a.Name == "Visa");
        creditResult.CurrentBalance.Should().Be(-1200m);

        var propertyResult = result.Single(a => a.Name == "House");
        propertyResult.CurrentBalance.Should().Be(850_000m);
    }

    [Fact]
    public async Task Handle_PropertyAccountsExcludedFromTransactionQuery()
    {
        var ownerId = Guid.NewGuid();
        var property = CreateAccount(
            ownerId,
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            770_000m
        );

        _accountRepositoryMock
            .Setup(x => x.GetByOwnerIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { property });

        _transactionRepositoryMock
            .Setup(x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => !ids.Any()),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new Dictionary<Guid, decimal>());

        var handler = new GetAccountsQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountsQuery(ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].CurrentBalance.Should().Be(770_000m);

        _transactionRepositoryMock.Verify(
            x =>
                x.GetSignedSumsByAccountIdsAsync(
                    It.Is<IEnumerable<Guid>>(ids => !ids.Any()),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    private static Account CreateAccount(
        Guid ownerId,
        string name,
        AccountType type,
        AccountSubType subType,
        decimal openingBalance
    ) =>
        new(
            name,
            type,
            subType,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2025, 1, 1),
            ownerId
        );

    private static void AddValuation(Account account, decimal amount, DateOnly effectiveDate)
    {
        var valuationsField = typeof(Account).GetField(
            "_valuations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        var valuations = (List<Valuation>)valuationsField.GetValue(account)!;
        valuations.Add(new Valuation(account.Id, new Money(amount), effectiveDate, "Appraisal"));
    }
}
