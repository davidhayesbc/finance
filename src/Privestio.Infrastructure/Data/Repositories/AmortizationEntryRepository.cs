using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class AmortizationEntryRepository : IAmortizationEntryRepository
{
    private readonly PrivestioDbContext _context;

    public AmortizationEntryRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AmortizationEntry>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .AmortizationEntries.Where(e => e.AccountId == accountId)
            .OrderBy(e => e.PaymentNumber)
            .ToListAsync(cancellationToken);

    public async Task DeleteByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await _context
            .AmortizationEntries.Where(e => e.AccountId == accountId)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.SoftDelete();
        }
    }

    public async Task AddRangeAsync(
        IEnumerable<AmortizationEntry> entries,
        CancellationToken cancellationToken = default
    )
    {
        await _context.AmortizationEntries.AddRangeAsync(entries, cancellationToken);
    }
}
