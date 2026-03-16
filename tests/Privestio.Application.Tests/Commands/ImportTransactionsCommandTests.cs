using FluentAssertions;
using Moq;
using Privestio.Application.Commands.ImportTransactions;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.Interfaces;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ImportTransactionsCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IImportBatchRepository> _importBatchRepo;
    private readonly Mock<IImportMappingRepository> _importMappingRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<IHoldingRepository> _holdingRepo;
    private readonly Mock<ILotRepository> _lotRepo;
    private readonly Mock<ITransactionImporter> _csvImporter;
    private readonly TransactionFingerprintService _fingerprintService;
    private readonly ImportTransactionsCommandHandler _handler;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ImportTransactionsCommandTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _importBatchRepo = new Mock<IImportBatchRepository>();
        _importMappingRepo = new Mock<IImportMappingRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _holdingRepo = new Mock<IHoldingRepository>();
        _lotRepo = new Mock<ILotRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork.Setup(u => u.Transactions).Returns(_transactionRepo.Object);
        _unitOfWork.Setup(u => u.ImportBatches).Returns(_importBatchRepo.Object);
        _unitOfWork.Setup(u => u.ImportMappings).Returns(_importMappingRepo.Object);
        _unitOfWork.Setup(u => u.Accounts).Returns(_accountRepo.Object);
        _unitOfWork.Setup(u => u.Holdings).Returns(_holdingRepo.Object);
        _unitOfWork.Setup(u => u.Lots).Returns(_lotRepo.Object);
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

        _holdingRepo
            .Setup(r => r.GetByAccountIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _holdingRepo
            .Setup(r => r.AddAsync(It.IsAny<Holding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holding h, CancellationToken _) => h);
        _holdingRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Holding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Holding h, CancellationToken _) => h);
        _lotRepo
            .Setup(r => r.AddAsync(It.IsAny<Lot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lot l, CancellationToken _) => l);

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
            _fingerprintService
        );
    }

    [Fact]
    public async Task Handle_ValidFile_ImportsTransactions()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
            new(DateTime.Parse("2025-01-17"), 2500.00m, "PAYROLL"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(2);
        result.DuplicateCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_DuplicateTransaction_SkipsDuplicates()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
            new(DateTime.Parse("2025-01-17"), 2500.00m, "PAYROLL"),
        };
        SetupParseResult(rows);

        // Make the first transaction's fingerprint already exist
        var existingFingerprint = _fingerprintService.GenerateFingerprint(
            _accountId,
            DateTime.Parse("2025-01-15"),
            new Domain.ValueObjects.Money(-42.99m),
            "GROCERY STORE"
        );
        _transactionRepo
            .Setup(r =>
                r.GetExistingFingerprintsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<string> { existingFingerprint } as IReadOnlySet<string>);

        var command = CreateCommand("transactions.csv");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(1);
        result.DuplicateCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_AllDuplicates_ImportsNone()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        SetupParseResult(rows);

        var existingFingerprint = _fingerprintService.GenerateFingerprint(
            _accountId,
            DateTime.Parse("2025-01-15"),
            new Domain.ValueObjects.Money(-42.99m),
            "GROCERY STORE"
        );
        _transactionRepo
            .Setup(r =>
                r.GetExistingFingerprintsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<string> { existingFingerprint } as IReadOnlySet<string>);

        var command = CreateCommand("transactions.csv");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(0);
        result.DuplicateCount.Should().Be(1);
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
    public async Task Handle_ParseErrors_ReportedInResult()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        var errors = new List<ImportRowError> { new(2, "Invalid date", "bad,row,data") };
        SetupParseResult(rows, errors);

        var command = CreateCommand("transactions.csv");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(1);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].RowNumber.Should().Be(2);
        result.Errors[0].Message.Should().Be("Invalid date");
        result.Status.Should().Be("CompletedWithErrors");
    }

    [Fact]
    public async Task Handle_UnsupportedFileFormat_ThrowsNotSupportedException()
    {
        var command = CreateCommand("data.xlsx");

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task Handle_NegativeAmount_CreatesDebitTransaction()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Transaction>>(txns =>
                        txns.First().Type == TransactionType.Debit
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_DebitTransaction_StoresAmountAsPositiveAbsoluteValue()
    {
        // Importers return signed amounts (negative for debits); the Transaction.Amount must
        // always be stored as a positive magnitude so that balance calculations don't
        // double-negate the value.
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Transaction>>(txns => txns.First().Amount.Amount == 42.99m),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CreditTransaction_StoresAmountAsPositiveAbsoluteValue()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-17"), 2500.00m, "PAYROLL"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Transaction>>(txns => txns.First().Amount.Amount == 2500.00m),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_PositiveAmount_CreatesCreditTransaction()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), 2500.00m, "PAYROLL"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Transaction>>(txns =>
                        txns.First().Type == TransactionType.Credit
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_SetsImportBatchIdOnTransactions()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        var result = await _handler.Handle(command, CancellationToken.None);

        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Transaction>>(txns =>
                        txns.All(t => t.ImportBatchId == result.ImportBatchId)
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CreatesBatchAndCallsSaveChanges()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -42.99m, "GROCERY STORE"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _importBatchRepo.Verify(
            r => r.AddAsync(It.IsAny<ImportBatch>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IdenticalSameDayTransactions_ImportsBothWithDistinctFingerprints()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -5.00m, "COFFEE SHOP"),
            new(DateTime.Parse("2025-01-15"), -5.00m, "COFFEE SHOP"),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(2);
        result.DuplicateCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Transaction>>(txns =>
                        txns.Select(t => t.ImportFingerprint).Distinct().Count() == 2
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_IdenticalSameDayTransactions_ReimportDetectsAllAsDuplicates()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-01-15"), -5.00m, "COFFEE SHOP"),
            new(DateTime.Parse("2025-01-15"), -5.00m, "COFFEE SHOP"),
        };
        SetupParseResult(rows);

        // Simulate both fingerprints already existing from a prior import
        var fp0 = _fingerprintService.GenerateFingerprint(
            _accountId,
            DateTime.Parse("2025-01-15"),
            new Domain.ValueObjects.Money(-5.00m),
            "COFFEE SHOP"
        );
        var fp1 = _fingerprintService.GenerateFingerprint(
            _accountId,
            DateTime.Parse("2025-01-15"),
            new Domain.ValueObjects.Money(-5.00m),
            "COFFEE SHOP",
            occurrenceIndex: 1
        );
        _transactionRepo
            .Setup(r =>
                r.GetExistingFingerprintsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new HashSet<string> { fp0, fp1 } as IReadOnlySet<string>);

        var command = CreateCommand("transactions.csv");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.ImportedCount.Should().Be(0);
        result.DuplicateCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ImportedInvestmentMetadata_PersistsTradeFields()
    {
        var rows = new List<ImportedTransactionRow>
        {
            new(
                DateTime.Parse("2025-02-03"),
                -75.00m,
                "Trade",
                SettlementDate: new DateOnly(2025, 2, 3),
                ActivityType: "Trade",
                ActivitySubType: "BUY",
                Direction: "LONG",
                Symbol: "XEQT",
                SecurityName: "iShares Core Equity ETF Portfolio",
                Quantity: 2.1361m,
                UnitPrice: 35.1098m
            ),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _transactionRepo.Verify(
            r =>
                r.AddRangeAsync(
                    It.Is<IEnumerable<Transaction>>(txns =>
                        txns.Any(t =>
                            t.SettlementDate == new DateOnly(2025, 2, 3)
                            && t.ActivityType == "Trade"
                            && t.ActivitySubType == "BUY"
                            && t.Direction == "LONG"
                            && t.Symbol == "XEQT"
                            && t.SecurityName == "iShares Core Equity ETF Portfolio"
                            && t.Quantity == 2.1361m
                            && t.UnitPrice == 35.1098m
                        )
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_InvestmentTrade_UpsertsHoldingAndLot()
    {
        var investmentAccount = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Domain.ValueObjects.Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            _userId
        );

        _accountRepo
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(investmentAccount);

        var rows = new List<ImportedTransactionRow>
        {
            new(
                DateTime.Parse("2025-02-03"),
                -75.00m,
                "Trade",
                SettlementDate: new DateOnly(2025, 2, 3),
                ActivityType: "Trade",
                ActivitySubType: "BUY",
                Direction: "LONG",
                Symbol: "XEQT",
                SecurityName: "iShares Core Equity ETF Portfolio",
                Quantity: 2.1361m,
                UnitPrice: 35.1098m
            ),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _holdingRepo.Verify(
            h =>
                h.AddAsync(
                    It.Is<Holding>(x => x.Symbol == "XEQT" && x.Quantity > 0),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _lotRepo.Verify(
            l => l.AddAsync(It.Is<Lot>(x => x.Quantity == 2.1361m), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CashDividendFollowedByCashBuy_TagsLotAsReinvestedIncome()
    {
        var investmentAccount = new Account(
            "TFSA",
            AccountType.Investment,
            AccountSubType.TFSA,
            "CAD",
            new Domain.ValueObjects.Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            _userId
        );

        _accountRepo
            .Setup(r => r.GetByIdAsync(_accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(investmentAccount);

        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.Parse("2025-10-07"), 0.4m, "Dividend", ActivityType: "Dividend"),
            new(
                DateTime.Parse("2025-10-08"),
                -0.4m,
                "Trade",
                SettlementDate: new DateOnly(2025, 10, 8),
                ActivityType: "Trade",
                ActivitySubType: "BUY",
                Direction: "LONG",
                Symbol: "CASH",
                SecurityName: "Global X High Interest Savings ETF",
                Quantity: 0.0079m,
                UnitPrice: 50.02m
            ),
        };
        SetupParseResult(rows);

        var command = CreateCommand("transactions.csv");
        await _handler.Handle(command, CancellationToken.None);

        _lotRepo.Verify(
            l =>
                l.AddAsync(
                    It.Is<Lot>(x => x.Quantity == 0.0079m && x.Source == "ReinvestedIncome"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    private ImportTransactionsCommand CreateCommand(string fileName) =>
        new(Stream.Null, fileName, _accountId, _userId);

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
