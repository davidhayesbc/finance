using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class PayeeRepository : IPayeeRepository
{
    private readonly PrivestioDbContext _context;

    public PayeeRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Payee?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Payees.Include(p => p.DefaultCategory)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Payee>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Payees.Where(p => p.OwnerId == ownerId)
            .Include(p => p.DefaultCategory)
            .OrderBy(p => p.DisplayName)
            .ToListAsync(cancellationToken);

    public async Task<Payee?> FindByAliasAsync(
        string rawPayee,
        Guid ownerId,
        CancellationToken cancellationToken = default
    )
    {
        var payees = await GetByOwnerIdAsync(ownerId, cancellationToken);
        return payees.FirstOrDefault(p => p.MatchesAlias(rawPayee));
    }

    public async Task<Payee> AddAsync(Payee payee, CancellationToken cancellationToken = default)
    {
        await _context.Payees.AddAsync(payee, cancellationToken);
        return payee;
    }

    public Task<Payee> UpdateAsync(Payee payee, CancellationToken cancellationToken = default)
    {
        _context.Payees.Update(payee);
        return Task.FromResult(payee);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payee = await GetByIdAsync(id, cancellationToken);
        if (payee is not null)
        {
            payee.SoftDelete();
        }
    }

    public async Task<bool> HasLinkedTransactionsAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.Transactions.AnyAsync(t => t.PayeeId == id, cancellationToken);
}
