using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class HoldingRepository : IHoldingRepository
{
    private readonly PrivestioDbContext _context;

    public HoldingRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Holding?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Holdings.Include(h => h.Lots)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Holding>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Holdings.Where(h => h.AccountId == accountId)
            .Include(h => h.Lots)
            .OrderBy(h => h.Symbol)
            .ToListAsync(cancellationToken);

    public async Task<Holding?> GetByAccountIdAndSymbolAsync(
        Guid accountId,
        string symbol,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Holdings.FirstOrDefaultAsync(
            h => h.AccountId == accountId && h.Symbol == symbol.ToUpperInvariant().Trim(),
            cancellationToken
        );

    public async Task<Holding> AddAsync(
        Holding holding,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Holdings.AddAsync(holding, cancellationToken);
        return holding;
    }

    public Task<Holding> UpdateAsync(Holding holding, CancellationToken cancellationToken = default)
    {
        _context.Holdings.Update(holding);
        return Task.FromResult(holding);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var holding = await GetByIdAsync(id, cancellationToken);
        if (holding is not null)
        {
            holding.SoftDelete();
        }
    }
}
