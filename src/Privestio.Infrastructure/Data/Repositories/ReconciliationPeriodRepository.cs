using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class ReconciliationPeriodRepository : IReconciliationPeriodRepository
{
    private readonly PrivestioDbContext _context;

    public ReconciliationPeriodRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<ReconciliationPeriod?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ReconciliationPeriods.Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReconciliationPeriod>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ReconciliationPeriods.Where(r => r.AccountId == accountId)
            .OrderByDescending(r => r.StatementDate)
            .ToListAsync(cancellationToken);

    public async Task<ReconciliationPeriod> AddAsync(
        ReconciliationPeriod period,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ReconciliationPeriods.AddAsync(period, cancellationToken);
        return period;
    }

    public async Task<ReconciliationPeriod> UpdateAsync(
        ReconciliationPeriod period,
        CancellationToken cancellationToken = default
    )
    {
        _context.ReconciliationPeriods.Update(period);
        return await Task.FromResult(period);
    }
}
