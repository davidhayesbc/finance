using Privestio.Application.Interfaces;
using Privestio.Infrastructure.Data.Repositories;

namespace Privestio.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly PrivestioDbContext _context;

    public UnitOfWork(PrivestioDbContext context)
    {
        _context = context;
        Accounts = new AccountRepository(context);
        Users = new UserRepository(context);
        Households = new HouseholdRepository(context);
        Transactions = new TransactionRepository(context);
        ImportBatches = new ImportBatchRepository(context);
        Categories = new CategoryRepository(context);
        Payees = new PayeeRepository(context);
        Tags = new TagRepository(context);
        CategorizationRules = new CategorizationRuleRepository(context);
        ImportMappings = new ImportMappingRepository(context);
        Budgets = new BudgetRepository(context);
        SinkingFunds = new SinkingFundRepository(context);
        RecurringTransactions = new RecurringTransactionRepository(context);
        Notifications = new NotificationRepository(context);
        ForecastScenarios = new ForecastScenarioRepository(context);
        ReconciliationPeriods = new ReconciliationPeriodRepository(context);
        ContributionRooms = new ContributionRoomRepository(context);
        AmortizationEntries = new AmortizationEntryRepository(context);
        ExchangeRates = new ExchangeRateRepository(context);
        FxConversions = new FxConversionRepository(context);
        SyncTombstones = new SyncTombstoneRepository(context);
        SyncCheckpoints = new SyncCheckpointRepository(context);
        SyncConflicts = new SyncConflictRepository(context);
        IdempotencyRecords = new IdempotencyRecordRepository(context);
        Valuations = new ValuationRepository(context);
        Securities = new SecurityRepository(context);
        PriceHistories = new PriceHistoryRepository(context);
        Holdings = new HoldingRepository(context);
        HoldingSnapshots = new HoldingSnapshotRepository(context);
        Lots = new LotRepository(context);
    }

    public IAccountRepository Accounts { get; }
    public IUserRepository Users { get; }
    public IHouseholdRepository Households { get; }
    public ITransactionRepository Transactions { get; }
    public IImportBatchRepository ImportBatches { get; }
    public ICategoryRepository Categories { get; }
    public IPayeeRepository Payees { get; }
    public ITagRepository Tags { get; }
    public ICategorizationRuleRepository CategorizationRules { get; }
    public IImportMappingRepository ImportMappings { get; }
    public IBudgetRepository Budgets { get; }
    public ISinkingFundRepository SinkingFunds { get; }
    public IRecurringTransactionRepository RecurringTransactions { get; }
    public INotificationRepository Notifications { get; }
    public IForecastScenarioRepository ForecastScenarios { get; }
    public IReconciliationPeriodRepository ReconciliationPeriods { get; }
    public IContributionRoomRepository ContributionRooms { get; }
    public IAmortizationEntryRepository AmortizationEntries { get; }
    public IExchangeRateRepository ExchangeRates { get; }
    public IFxConversionRepository FxConversions { get; }
    public ISyncTombstoneRepository SyncTombstones { get; }
    public ISyncCheckpointRepository SyncCheckpoints { get; }
    public ISyncConflictRepository SyncConflicts { get; }
    public IIdempotencyRecordRepository IdempotencyRecords { get; }
    public IValuationRepository Valuations { get; }
    public ISecurityRepository Securities { get; }
    public IPriceHistoryRepository PriceHistories { get; }
    public IHoldingRepository Holdings { get; }
    public IHoldingSnapshotRepository HoldingSnapshots { get; }
    public ILotRepository Lots { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
