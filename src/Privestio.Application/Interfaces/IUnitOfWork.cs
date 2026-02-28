namespace Privestio.Application.Interfaces;

public interface IUnitOfWork
{
    IAccountRepository Accounts { get; }
    ITransactionRepository Transactions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
