using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
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
            .Include(s => s.Identifiers)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Security?> GetByAnySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = SecuritySymbolMatcher.Normalize(symbol);

        return await _context
            .Securities.Include(s => s.Aliases)
            .Include(s => s.Identifiers)
            .FirstOrDefaultAsync(
                s =>
                    s.CanonicalSymbol == normalized
                    || s.DisplaySymbol == normalized
                    || s.Aliases.Any(a => a.Symbol == normalized),
                cancellationToken
            );
    }

    public async Task<Security?> GetByIdentifierAsync(
        SecurityIdentifierType identifierType,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = value.Trim().ToUpperInvariant();

        return await _context
            .Securities.Include(s => s.Aliases)
            .Include(s => s.Identifiers)
            .FirstOrDefaultAsync(
                s =>
                    s.Identifiers.Any(i =>
                        i.IdentifierType == identifierType && i.Value == normalized
                    ),
                cancellationToken
            );
    }

    public async Task<Security?> GetByAliasContextAsync(
        string symbol,
        string? source,
        string? exchange,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedSymbol = SecuritySymbolMatcher.Normalize(symbol);
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        var normalizedExchange = string.IsNullOrWhiteSpace(exchange)
            ? null
            : exchange.Trim().ToUpperInvariant();

        return await _context
            .Securities.Include(s => s.Aliases)
            .Include(s => s.Identifiers)
            .FirstOrDefaultAsync(
                s =>
                    s.Aliases.Any(a =>
                        a.Symbol == normalizedSymbol
                        && (normalizedSource == null || a.Source == normalizedSource)
                        && (normalizedExchange == null || a.Exchange == normalizedExchange)
                    ),
                cancellationToken
            );
    }

    public async Task<IReadOnlyList<Security>> GetCandidatesBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = SecuritySymbolMatcher.Normalize(symbol);

        return await _context
            .Securities.Include(s => s.Aliases)
            .Include(s => s.Identifiers)
            .Where(s =>
                s.CanonicalSymbol == normalized
                || s.DisplaySymbol == normalized
                || s.Aliases.Any(a => a.Symbol == normalized)
            )
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Security>> GetByIdsAsync(
        IReadOnlyCollection<Guid> securityIds,
        CancellationToken cancellationToken = default
    )
    {
        if (securityIds.Count == 0)
            return [];

        return await _context
            .Securities.Include(s => s.Aliases)
            .Include(s => s.Identifiers)
            .Where(s => securityIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
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
        // Disable AutoDetectChanges so that reading entry states below does not
        // trigger DetectChanges prematurely.  When DetectChanges runs before we
        // have a chance to register new child entities, EF Core can incorrectly
        // track newly created SecurityAlias / SecurityIdentifier instances
        // (which have client-generated non-default GUIDs) as EntityState.Modified
        // instead of EntityState.Added, causing UPDATE … WHERE Version = 0 statements
        // that find no row and throw DbUpdateConcurrencyException.
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            var entry = _context.Entry(security);

            if (entry.State == EntityState.Detached)
            {
                // Entity was not loaded via a tracking query; let EF graph-update it.
                _context.Securities.Update(security);
                return Task.FromResult(security);
            }

            // Entity is already tracked (loaded via GetByIdAsync).
            // Explicitly register any child entities that are not yet tracked so that
            // EF Core starts them as Added rather than accidentally as Modified.
            foreach (var alias in security.Aliases)
            {
                if (_context.Entry(alias).State == EntityState.Detached)
                    _context.Entry(alias).State = EntityState.Added;
            }

            foreach (var identifier in security.Identifiers)
            {
                if (_context.Entry(identifier).State == EntityState.Detached)
                    _context.Entry(identifier).State = EntityState.Added;
            }

            return Task.FromResult(security);
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
