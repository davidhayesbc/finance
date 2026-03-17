using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Services;

namespace Privestio.Infrastructure.Data.Repositories;

public class SecurityRepository : ISecurityRepository
{
    private readonly PrivestioDbContext _context;

    public SecurityRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Security?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Securities.Include(s => s.Aliases)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Security?> GetByAnySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = SecuritySymbolMatcher.Normalize(symbol);

        return await _context
            .Securities.Include(s => s.Aliases)
            .FirstOrDefaultAsync(
                s =>
                    s.CanonicalSymbol == normalized
                    || s.DisplaySymbol == normalized
                    || s.Aliases.Any(a => a.Symbol == normalized),
                cancellationToken
            );
    }

    public async Task<Security> AddAsync(
        Security security,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Securities.AddAsync(security, cancellationToken);
        return security;
    }

    public Task<Security> UpdateAsync(
        Security security,
        CancellationToken cancellationToken = default
    )
    {
        _context.Securities.Update(security);
        return Task.FromResult(security);
    }
}
