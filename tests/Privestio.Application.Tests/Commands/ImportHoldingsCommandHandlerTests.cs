using FluentAssertions;
using Moq;
using Privestio.Application.Commands.ImportHoldings;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ImportHoldingsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<IHoldingRepository> _holdingRepo;
    private readonly Mock<IImportBatchRepository> _importBatchRepo;
    private readonly Mock<IHoldingsImporter> _pdfImporter;
    private readonly ImportHoldingsCommandHandler _handler;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ImportHoldingsCommandHandlerTests()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _holdingRepo = new Mock<IHoldingRepository>();
        _importBatchRepo = new Mock<IImportBatchRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepo.Object);
        _unitOfWork.Setup(u => u.Holdings).Returns(_holdingRepo.Object);
        _unitOfWork.Setup(u => u.ImportBatches).Returns(_importBatchRepo.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var account = new Account(
            "RRSP",
            AccountType.Investment,
            AccountSubType.RRSP,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            _userId
        );

        _accountRepo
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _holdingRepo
            .Setup(r => r.AddAsync(It.IsAny<Holding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holding h, CancellationToken _) => h);
        _holdingRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Holding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holding h, CancellationToken _) => h);

        _importBatchRepo
            .Setup(r => r.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch b, CancellationToken _) => b);

        _pdfImporter = new Mock<IHoldingsImporter>();
        _pdfImporter.Setup(i => i.CanHandle("statement.pdf")).Returns(true);
        _pdfImporter.Setup(i => i.FileFormat).Returns("PDF");

        _handler = new ImportHoldingsCommandHandler(
            _unitOfWork.Object,
            [_pdfImporter.Object],
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWork)
        );
    }

    [Fact]
    public async Task Handle_ValidPdf_CreatesNewHoldings()
    {
        var holdings = new List<ImportedHoldingRow>
        {
            new("Sun Life Growth Fund", 100.000m, 15.50m, 1550.00m),
            new("Sun Life Bond Fund", 200.000m, 10.00m, 2000.00m),
        };
        SetupParseResult(holdings);

        _holdingRepo
            .Setup(r =>
                r.GetByAccountIdAndSecurityIdAsync(
                    _accountId,
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Holding?)null);

        var command = CreateCommand("statement.pdf");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.TotalHoldings.Should().Be(2);
        result.CreatedCount.Should().Be(2);
        result.UpdatedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_ExistingHolding_UpdatesRatherThanCreates()
    {
        var holdings = new List<ImportedHoldingRow>
        {
            new("Sun Life Growth Fund", 150.000m, 16.00m, 2400.00m, Symbol: "SLGF"),
        };
        SetupParseResult(holdings);

        var existingSecurity = SecurityTestHelper.CreateSecurity("SLGF", "Sun Life Growth Fund");

        _unitOfWork.Setup(u => u.Securities).Returns(SetupSecurityRepo(existingSecurity));

        var existingHolding = new Holding(
            _accountId,
            existingSecurity.Id,
            "SLGF",
            "Sun Life Growth Fund",
            100.000m,
            new Money(15.50m)
        );

        _holdingRepo
            .Setup(r =>
                r.GetByAccountIdAndSecurityIdAsync(
                    _accountId,
                    existingSecurity.Id,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(existingHolding);

        // Recreate handler with the updated security resolution service
        var handler = new ImportHoldingsCommandHandler(
            _unitOfWork.Object,
            [_pdfImporter.Object],
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWork, [existingSecurity])
        );

        var command = CreateCommand("statement.pdf");
        var result = await handler.Handle(command, CancellationToken.None);

        result.CreatedCount.Should().Be(0);
        result.UpdatedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WrongUser_ThrowsUnauthorized()
    {
        var command = new ImportHoldingsCommand(
            new MemoryStream(),
            "statement.pdf",
            _accountId,
            Guid.NewGuid() // different user
        );

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_NonExistentAccount_ThrowsKeyNotFound()
    {
        _accountRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var command = CreateCommand("statement.pdf");

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_UnsupportedFile_ThrowsNotSupported()
    {
        var command = new ImportHoldingsCommand(
            new MemoryStream(),
            "statement.xlsx",
            _accountId,
            _userId
        );

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task Handle_ParseErrors_PropagatedToResponse()
    {
        var holdings = new List<ImportedHoldingRow>
        {
            new("Good Fund", 100.000m, 10.00m, 1000.00m),
        };
        var errors = new List<ImportRowError> { new(2, "Failed to parse numeric values") };
        SetupParseResult(holdings, errors);

        _holdingRepo
            .Setup(r =>
                r.GetByAccountIdAndSecurityIdAsync(
                    _accountId,
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Holding?)null);

        var command = CreateCommand("statement.pdf");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ErrorCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Status.Should().Be("CompletedWithErrors");
    }

    [Fact]
    public async Task Handle_StatementDateFromCommand_OverridesExtracted()
    {
        var holdings = new List<ImportedHoldingRow>
        {
            new("Test Fund", 100.000m, 10.00m, 1000.00m),
        };
        SetupParseResult(holdings, statementDate: new DateOnly(2024, 6, 30));

        _holdingRepo
            .Setup(r =>
                r.GetByAccountIdAndSecurityIdAsync(
                    _accountId,
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((Holding?)null);

        var overrideDate = new DateOnly(2024, 12, 31);
        var command = new ImportHoldingsCommand(
            new MemoryStream(),
            "statement.pdf",
            _accountId,
            _userId,
            overrideDate
        );

        var result = await _handler.Handle(command, CancellationToken.None);

        result.StatementDate.Should().Be(overrideDate);
    }

    private void SetupParseResult(
        IReadOnlyList<ImportedHoldingRow> holdings,
        IReadOnlyList<ImportRowError>? errors = null,
        DateOnly? statementDate = null
    )
    {
        var parseResult = new HoldingsParseResult(
            statementDate ?? new DateOnly(2024, 12, 31),
            holdings,
            errors ?? []
        );

        _pdfImporter
            .Setup(i =>
                i.ParseAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(parseResult);
    }

    private ImportHoldingsCommand CreateCommand(string fileName) =>
        new(new MemoryStream(), fileName, _accountId, _userId);

    private static ISecurityRepository SetupSecurityRepo(params Security[] securities)
    {
        var repo = new Mock<ISecurityRepository>();
        var list = securities.ToList();

        repo.Setup(r => r.GetByAnySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (string symbol, CancellationToken _) =>
                    list.FirstOrDefault(s =>
                        string.Equals(s.CanonicalSymbol, symbol, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(
                            s.DisplaySymbol,
                            symbol,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
            );

        repo.Setup(r => r.AddAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (Security s, CancellationToken _) =>
                {
                    list.Add(s);
                    return s;
                }
            );

        repo.Setup(r => r.UpdateAsync(It.IsAny<Security>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Security s, CancellationToken _) => s);

        return repo.Object;
    }
}
