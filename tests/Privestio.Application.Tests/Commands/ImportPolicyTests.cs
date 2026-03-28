using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Privestio.Application.Commands.ImportTransactions;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Application.Tests;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ImportPolicyTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IImportBatchRepository> _importBatchRepo;
    private readonly Mock<IImportMappingRepository> _importMappingRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<ITransactionImporter> _csvImporter;
    private readonly TransactionFingerprintService _fingerprintService;
    private readonly ImportTransactionsCommandHandler _handler;
    private readonly Mock<IPriceFeedProvider> _priceFeedProvider = new();

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ImportPolicyTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _importBatchRepo = new Mock<IImportBatchRepository>();
        _importMappingRepo = new Mock<IImportMappingRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.Setup(u => u.Transactions).Returns(_transactionRepo.Object);
        _unitOfWork.Setup(u => u.ImportBatches).Returns(_importBatchRepo.Object);
        _unitOfWork.Setup(u => u.ImportMappings).Returns(_importMappingRepo.Object);
        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepo.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var account = new Account(
            "Chequing",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Domain.ValueObjects.Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            _userId
        );
        _accountRepo
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _importBatchRepo
            .Setup(r => r.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch b, CancellationToken _) => b);
        _importBatchRepo
            .Setup(r => r.UpdateAsync(It.IsAny<ImportBatch>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImportBatch b, CancellationToken _) => b);

        _transactionRepo
            .Setup(r =>
                r.GetExistingFingerprintsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<string>() as IReadOnlySet<string>);

        _csvImporter = new Mock<ITransactionImporter>();
        _csvImporter.Setup(i => i.CanHandle("transactions.csv")).Returns(true);
        _csvImporter.Setup(i => i.FileFormat).Returns("CSV");

        _fingerprintService = new TransactionFingerprintService();

        _handler = new ImportTransactionsCommandHandler(
            _unitOfWork.Object,
            [_csvImporter.Object],
            _fingerprintService,
            SecurityTestHelper.CreateSecurityResolutionService(_unitOfWork),
            _priceFeedProvider.Object,
            Options.Create(new PricingOptions()),
            NullLogger<ImportTransactionsCommandHandler>.Instance
        );
    }

    [Fact]
    public async Task Handle_SkipInvalidPolicy_SkipsErrorRowsAndImportsValid()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        var errors = new List<ImportRowError> { new(2, "Invalid date", "bad,row") };
        SetupParseResult(rows, errors);

        var command = CreateCommand("transactions.csv", ImportPolicy.SkipInvalid);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.Status.Should().Be("CompletedWithErrors");
    }

    [Fact]
    public async Task Handle_FailFastPolicy_AbortsOnErrors()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        var errors = new List<ImportRowError> { new(2, "Invalid date", "bad,row") };
        SetupParseResult(rows, errors);

        var command = CreateCommand("transactions.csv", ImportPolicy.FailFast);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Status.Should().Be("Failed");
        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<Transaction>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_FailFastPolicy_NoErrors_ImportsNormally()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv", ImportPolicy.FailFast);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_PreviewOnlyPolicy_DoesNotPersistTransactions()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
            new(DateTime.Parse("2025-01-17"), 2500.00m, "PAYROLL"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv", ImportPolicy.PreviewOnly);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(2);
        result.Status.Should().Contain("Preview");
        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.IsAny<IEnumerable<Transaction>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DefaultPolicy_IsSkipInvalid()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        var errors = new List<ImportRowError> { new(2, "Invalid date", "bad,row") };
        SetupParseResult(rows, errors);

        // Default policy (no explicit policy => SkipInvalid)
        var command = new ImportTransactionsCommand(
            Stream.Null,
            "transactions.csv",
            _accountId,
            _userId
        );
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
    }

    private ImportTransactionsCommand CreateCommand(string fileName, ImportPolicy policy) =>
        new(Stream.Null, fileName, _accountId, _userId, Policy: policy);

    private void SetupParseResult(
        List<ImportedTransactionRow> rows,
        List<ImportRowError>? errors = null
    )
    {
        _csvImporter
            .Setup(i =>
                i.ParseAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<ImportMapping?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new ImportParseResult(rows, errors ?? []));
    }
}
