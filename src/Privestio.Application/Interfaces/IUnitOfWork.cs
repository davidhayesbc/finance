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
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
