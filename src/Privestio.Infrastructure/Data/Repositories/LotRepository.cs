using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class LotRepository : ILotRepository
{
    private readonly PrivestioDbContext _context;

    public LotRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Lot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Lots.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Lot>> GetByHoldingIdAsync(
        Guid holdingId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Lots.Where(l => l.HoldingId == holdingId)
            .OrderBy(l => l.AcquiredDate)
            .ToListAsync(cancellationToken);

    public async Task<DateOnly?> GetEarliestAcquiredDateBySecurityIdAsync(
        Guid securityId,
        CancellationToken cancellationToken = default
    )
    {
        var date = await _context
            .Lots.Where(l => l.Holding != null && l.Holding.SecurityId == securityId)
            .MinAsync(l => (DateOnly?)l.AcquiredDate, cancellationToken);
        return date;
    }

    public async Task<Lot> AddAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        await _context.Lots.AddAsync(lot, cancellationToken);
        return lot;
    }

    public Task<Lot> UpdateAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        _context.Lots.Update(lot);
        return Task.FromResult(lot);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lot = await GetByIdAsync(id, cancellationToken);
        if (lot is not null)
        {
            lot.SoftDelete();
        }
    }
}
