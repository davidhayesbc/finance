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
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
