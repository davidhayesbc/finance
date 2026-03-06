using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Queries.GetImportMappings;
using Privestio.Domain.Entities;
using Xunit;

namespace Privestio.Application.Tests.Queries;

public class GetImportMappingsQueryTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IImportMappingRepository> _mappingRepoMock;
    private readonly GetImportMappingsQueryHandler _handler;

    public GetImportMappingsQueryTests()
    {
        _mappingRepoMock = new Mock<IImportMappingRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.ImportMappings).Returns(_mappingRepoMock.Object);

        _handler = new GetImportMappingsQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappingsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mappings = new List<ImportMapping>
        {
            new(
                "RBC CSV",
                "CSV",
                userId,
                new Dictionary<string, string> { ["Date"] = "Date", ["Amount"] = "Amount" },
                "RBC"
            ),
            new(
                "TD OFX",
                "OFX",
                userId,
                new Dictionary<string, string> { ["Date"] = "Date" },
                "TD"
            ),
        };

        _mappingRepoMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);

        var query = new GetImportMappingsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("RBC CSV");
        result[0].FileFormat.Should().Be("CSV");
        result[0].Institution.Should().Be("RBC");
        result[1].Name.Should().Be("TD OFX");
    }

    [Fact]
    public async Task Handle_NoMappings_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mappingRepoMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportMapping>());

        var query = new GetImportMappingsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
