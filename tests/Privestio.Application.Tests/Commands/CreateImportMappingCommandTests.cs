using FluentAssertions;
using Moq;
using Privestio.Application.Commands.CreateImportMapping;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class CreateImportMappingCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IImportMappingRepository> _mappingRepoMock;
    private readonly Mock<IPluginRegistryService> _pluginRegistryMock;
    private readonly CreateImportMappingCommandHandler _handler;

    public CreateImportMappingCommandTests()
    {
        _mappingRepoMock = new Mock<IImportMappingRepository>();
        _pluginRegistryMock = new Mock<IPluginRegistryService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ImportMappings).Returns(_mappingRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _pluginRegistryMock
            .Setup(p => p.IsRegisteredTransactionImportFormat(It.IsAny<string>()))
            .Returns(true);

        _mappingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ImportMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportMapping m, CancellationToken _) => m);

        _handler = new CreateImportMappingCommandHandler(
            _unitOfWorkMock.Object,
            _pluginRegistryMock.Object
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsImportMappingResponse()
    {
        // Arrange
        var command = new CreateImportMappingCommand(
            Name: "RBC Chequing CSV",
            FileFormat: "CSV",
            UserId: Guid.NewGuid(),
            ColumnMappings: new Dictionary<string, string>
            {
                ["Date"] = "Date",
                ["Amount"] = "Amount",
                ["Description 1"] = "Description",
            },
            Institution: "RBC"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("RBC Chequing CSV");
        result.FileFormat.Should().Be("CSV");
        result.Institution.Should().Be("RBC");
        result.ColumnMappings.Should().HaveCount(3);
        result.HasHeaderRow.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        // Arrange
        var command = new CreateImportMappingCommand(
            Name: "Test Mapping",
            FileFormat: "CSV",
            UserId: Guid.NewGuid(),
            ColumnMappings: new Dictionary<string, string> { ["Date"] = "Date" }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mappingRepoMock.Verify(
            r => r.AddAsync(It.IsAny<ImportMapping>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithOptionalFields_SetsFieldsCorrectly()
    {
        // Arrange
        var command = new CreateImportMappingCommand(
            Name: "Custom Mapping",
            FileFormat: "csv",
            UserId: Guid.NewGuid(),
            ColumnMappings: new Dictionary<string, string> { ["Date"] = "Date" },
            DateFormat: "yyyy-MM-dd",
            HasHeaderRow: false,
            AmountDebitColumn: "Debit",
            AmountCreditColumn: "Credit"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.DateFormat.Should().Be("yyyy-MM-dd");
        result.HasHeaderRow.Should().BeFalse();
        result.AmountDebitColumn.Should().Be("Debit");
        result.AmountCreditColumn.Should().Be("Credit");
    }
}
