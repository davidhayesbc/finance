using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateExchangeRate;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ExchangeRateCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IExchangeRateRepository> _exchangeRateRepoMock;

    public ExchangeRateCommandTests()
    {
        _exchangeRateRepoMock = new Mock<IExchangeRateRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ExchangeRates).Returns(_exchangeRateRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task CreateExchangeRate_ValidCommand_ReturnsResponse()
    {
        // Arrange
        _exchangeRateRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExchangeRate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExchangeRate e, CancellationToken _) => e);

        var handler = new CreateExchangeRateCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateExchangeRateCommand(
            "CAD",
            "USD",
            0.74m,
            new DateOnly(2025, 6, 15),
            "Bank of Canada",
            Guid.NewGuid()
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FromCurrency.Should().Be("CAD");
        result.ToCurrency.Should().Be("USD");
        result.Rate.Should().Be(0.74m);
        result.AsOfDate.Should().Be(new DateOnly(2025, 6, 15));
        result.Source.Should().Be("Bank of Canada");
    }

    [Fact]
    public async Task CreateExchangeRate_CallsSaveChanges()
    {
        // Arrange
        _exchangeRateRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExchangeRate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExchangeRate e, CancellationToken _) => e);

        var handler = new CreateExchangeRateCommandHandler(_unitOfWorkMock.Object);
        var command = new CreateExchangeRateCommand(
            "USD",
            "CAD",
            1.35m,
            new DateOnly(2025, 6, 15),
            "Manual",
            Guid.NewGuid()
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _exchangeRateRepoMock.Verify(
            r => r.AddAsync(It.IsAny<ExchangeRate>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
