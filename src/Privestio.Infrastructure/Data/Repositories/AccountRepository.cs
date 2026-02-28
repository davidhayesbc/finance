using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly PrivestioDbContext _context;

    public AccountRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Accounts
            .Include(a => a.Valuations)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Account>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
        => await _context.Accounts
            .Where(a => a.OwnerId == ownerId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public async Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
        return account;
    }

    public async Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        return await Task.FromResult(account);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await GetByIdAsync(id, cancellationToken);
        if (account is not null)
        {
            account.SoftDelete();
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Accounts.AnyAsync(a => a.Id == id, cancellationToken);
}
