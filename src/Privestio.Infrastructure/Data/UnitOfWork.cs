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
        Transactions = new TransactionRepository(context);
    }

    public IAccountRepository Accounts { get; }
    public ITransactionRepository Transactions { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
