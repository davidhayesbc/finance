using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class CategorizationRuleRepository : ICategorizationRuleRepository
{
    private readonly PrivestioDbContext _context;

    public CategorizationRuleRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<CategorizationRule?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Set<CategorizationRule>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CategorizationRule>> GetEnabledByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Set<CategorizationRule>()
            .Where(r => r.UserId == userId && r.IsEnabled)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategorizationRule>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Set<CategorizationRule>()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

    public async Task<CategorizationRule> AddAsync(
        CategorizationRule rule,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Set<CategorizationRule>().AddAsync(rule, cancellationToken);
        return rule;
    }

    public Task<CategorizationRule> UpdateAsync(
        CategorizationRule rule,
        CancellationToken cancellationToken = default
    )
    {
        _context.Set<CategorizationRule>().Update(rule);
        return Task.FromResult(rule);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await GetByIdAsync(id, cancellationToken);
        if (rule is not null)
        {
            rule.SoftDelete();
        }
    }
}
