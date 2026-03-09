using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class ValuationRepository : IValuationRepository
{
    private readonly PrivestioDbContext _context;

    public ValuationRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Valuation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.Valuations.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Valuation>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Valuations.Where(v => v.AccountId == accountId)
            .OrderByDescending(v => v.EffectiveDate)
            .ToListAsync(cancellationToken);

    public async Task<Valuation?> GetLatestByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Valuations.Where(v => v.AccountId == accountId)
            .OrderByDescending(v => v.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Valuation> AddAsync(
        Valuation valuation,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Valuations.AddAsync(valuation, cancellationToken);
        return valuation;
    }

    public async Task<bool> ExistsAsync(
        Guid accountId,
        DateOnly effectiveDate,
        string source,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Valuations.AnyAsync(
            v => v.AccountId == accountId && v.EffectiveDate == effectiveDate && v.Source == source,
            cancellationToken
        );
}
