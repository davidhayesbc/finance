using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetAccountById;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetAccountByIdQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;

    public GetAccountByIdQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();

        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.Transactions).Returns(_transactionRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsNull()
    {
        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new GetAccountByIdQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AccountNotOwned_ReturnsNull()
    {
        var ownerId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = CreateBankingAccount(ownerId, openingBalance: 1000m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GetAccountByIdQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountByIdQuery(accountId, requestingUserId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BankingAccount_CurrentBalanceIsOpeningPlusTransactions()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateBankingAccount(ownerId, openingBalance: 500m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x => x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(250m);

        var handler = new GetAccountByIdQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(750m);
    }

    [Fact]
    public async Task Handle_BankingAccount_NoTransactions_CurrentBalanceIsOpeningBalance()
    {
        var ownerId = Guid.NewGuid();
        var account = CreateBankingAccount(ownerId, openingBalance: 1000m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepositoryMock
            .Setup(x => x.GetSignedSumByAccountIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var handler = new GetAccountByIdQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_PropertyAccount_CurrentBalanceIsLatestValuation()
    {
        var ownerId = Guid.NewGuid();
        var account = CreatePropertyAccount(ownerId, openingBalance: 770_000m);
        AddValuation(account, 800_000m, new DateOnly(2025, 6, 1));
        AddValuation(account, 850_000m, new DateOnly(2026, 1, 1));
        AddValuation(account, 820_000m, new DateOnly(2025, 9, 1));

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GetAccountByIdQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(850_000m);

        _transactionRepositoryMock.Verify(
            x => x.GetSignedSumByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_PropertyAccount_NoValuations_FallsBackToOpeningBalance()
    {
        var ownerId = Guid.NewGuid();
        var account = CreatePropertyAccount(ownerId, openingBalance: 770_000m);

        _accountRepositoryMock
            .Setup(x => x.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var handler = new GetAccountByIdQueryHandler(_unitOfWorkMock.Object);
        var query = new GetAccountByIdQuery(account.Id, ownerId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.CurrentBalance.Should().Be(770_000m);
    }

    private static Account CreateBankingAccount(Guid ownerId, decimal openingBalance) =>
        new(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2025, 1, 1),
            ownerId
        );

    private static Account CreatePropertyAccount(Guid ownerId, decimal openingBalance) =>
        new(
            "House",
            AccountType.Property,
            AccountSubType.RealEstate,
            "CAD",
            new Money(openingBalance),
            new DateOnly(2020, 6, 1),
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
