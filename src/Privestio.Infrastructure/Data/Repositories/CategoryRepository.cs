using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly PrivestioDbContext _context;

    public CategoryRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Categories.Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Categories.Where(c => c.OwnerId == ownerId || c.IsSystem)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<Category> AddAsync(
        Category category,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Categories.AddAsync(category, cancellationToken);
        return category;
    }

    public Task<Category> UpdateAsync(
        Category category,
        CancellationToken cancellationToken = default
    )
    {
        _context.Categories.Update(category);
        return Task.FromResult(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetByIdAsync(id, cancellationToken);
        if (category is not null)
        {
            category.SoftDelete();
        }
    }

    public async Task<bool> HasLinkedTransactionsAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Transactions.AnyAsync(t => t.CategoryId == id, cancellationToken)
        || await _context.TransactionSplits.AnyAsync(s => s.CategoryId == id, cancellationToken);
}
