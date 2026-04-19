using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetAccountValueHistory;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Queries;

public class GetAccountValueHistoryQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IExchangeRateProvider> _exchangeRateProviderMock;
    private readonly HistoricalValueTimelineService _timelineService;

    public GetAccountValueHistoryQueryTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _exchangeRateProviderMock = new Mock<IExchangeRateProvider>();

        _unitOfWorkMock.SetupGet(x => x.Accounts).Returns(_accountRepositoryMock.Object);

        _exchangeRateProviderMock.SetupGet(x => x.ProviderName).Returns("Frankfurter");

        _timelineService = new HistoricalValueTimelineService(
            _unitOfWorkMock.Object,
            _exchangeRateProviderMock.Object
        );
    }

    [Fact]
    public async Task Handle_AccountNotAccessible_ReturnsNull()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _accountRepositoryMock
            .Setup(x => x.GetAccessibleByIdAsync(accountId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var handler = new GetAccountValueHistoryQueryHandler(_unitOfWorkMock.Object, _timelineService);

        var result = await handler.Handle(
            new GetAccountValueHistoryQuery(
                accountId,
                userId,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31)
            ),
            CancellationToken.None
        );

        result.Should().BeNull();
        _accountRepositoryMock.Verify(
            x => x.GetAccessibleByIdAsync(accountId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _accountRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_PropertyAccount_ReturnsEstimatedValueLabel()
    {
        var ownerId = Guid.NewGuid();
        var account = new Account(
            "Property",
            AccountType.Property,
            AccountSubType.RealEstate,
            "CAD",
            new Money(700000m, "CAD"),
            new DateOnly(2020, 1, 1),
            ownerId
        );

        var valuation = new Valuation(
            account.Id,
            new Money(850000m, "CAD"),
            new DateOnly(2026, 1, 15),
            "Appraisal"
        );

        var valuationsField = typeof(Account).GetField(
            "_valuations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        var valuations = (List<Valuation>)valuationsField!.GetValue(account)!;
        valuations.Add(valuation);

        _accountRepositoryMock
            .Setup(x =>
                x.GetAccessibleByIdAsync(account.Id, ownerId, It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(account);

        var handler = new GetAccountValueHistoryQueryHandler(_unitOfWorkMock.Object, _timelineService);

        var result = await handler.Handle(
            new GetAccountValueHistoryQuery(
                account.Id,
                ownerId,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31)
            ),
            CancellationToken.None
        );

        result.Should().NotBeNull();
        result!.Label.Should().Be("Estimated Value");
        result.Points.Should().NotBeEmpty();
    }
}
