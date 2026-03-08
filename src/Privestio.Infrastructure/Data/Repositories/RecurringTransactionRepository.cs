using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class RecurringTransactionRepository : IRecurringTransactionRepository
{
    private readonly PrivestioDbContext _context;

    public RecurringTransactionRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<RecurringTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .RecurringTransactions.Include(r => r.Account)
            .Include(r => r.Category)
            .Include(r => r.Payee)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RecurringTransaction>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .RecurringTransactions.Where(r => r.UserId == userId)
            .Include(r => r.Account)
            .Include(r => r.Category)
            .Include(r => r.Payee)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RecurringTransaction>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .RecurringTransactions.Where(r => r.UserId == userId && r.IsActive)
            .Include(r => r.Account)
            .Include(r => r.Category)
            .Include(r => r.Payee)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RecurringTransaction>> GetDueBeforeAsync(
        DateTime date,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .RecurringTransactions.Where(r => r.IsActive && r.NextOccurrence <= date)
            .Include(r => r.Account)
            .Include(r => r.Category)
            .Include(r => r.Payee)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(cancellationToken);

    public async Task<RecurringTransaction> AddAsync(
        RecurringTransaction recurring,
        CancellationToken cancellationToken = default
    )
    {
        await _context.RecurringTransactions.AddAsync(recurring, cancellationToken);
        return recurring;
    }

    public async Task<RecurringTransaction> UpdateAsync(
        RecurringTransaction recurring,
        CancellationToken cancellationToken = default
    )
    {
        _context.RecurringTransactions.Update(recurring);
        return await Task.FromResult(recurring);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recurring = await GetByIdAsync(id, cancellationToken);
        if (recurring is not null)
        {
            recurring.SoftDelete();
        }
    }
}
