namespace Privestio.Application.Interfaces;

public interface IUnitOfWork
{
    IAccountRepository Accounts { get; }
    ITransactionRepository Transactions { get; }
    IImportBatchRepository ImportBatches { get; }
    ICategoryRepository Categories { get; }
    IPayeeRepository Payees { get; }
    ITagRepository Tags { get; }
    ICategorizationRuleRepository CategorizationRules { get; }
    IImportMappingRepository ImportMappings { get; }
    IBudgetRepository Budgets { get; }
    ISinkingFundRepository SinkingFunds { get; }
    IRecurringTransactionRepository RecurringTransactions { get; }
    INotificationRepository Notifications { get; }
    IForecastScenarioRepository ForecastScenarios { get; }
    IReconciliationPeriodRepository ReconciliationPeriods { get; }
    IContributionRoomRepository ContributionRooms { get; }
    IAmortizationEntryRepository AmortizationEntries { get; }
    IExchangeRateRepository ExchangeRates { get; }
    IFxConversionRepository FxConversions { get; }
    ISyncTombstoneRepository SyncTombstones { get; }
    ISyncCheckpointRepository SyncCheckpoints { get; }
    ISyncConflictRepository SyncConflicts { get; }
    IIdempotencyRecordRepository IdempotencyRecords { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
