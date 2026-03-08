using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly PrivestioDbContext _context;

    public BudgetRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Budget?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Budgets.Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Budget>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Budgets.Where(b => b.UserId == userId)
            .Include(b => b.Category)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Budget>> GetByUserIdAndPeriodAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Budgets.Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Include(b => b.Category)
            .OrderBy(b => b.Category!.Name)
            .ToListAsync(cancellationToken);

    public async Task<Budget?> GetByUserCategoryPeriodAsync(
        Guid userId,
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Budgets.FirstOrDefaultAsync(
            b =>
                b.UserId == userId
                && b.CategoryId == categoryId
                && b.Year == year
                && b.Month == month,
            cancellationToken
        );

    public async Task<Budget> AddAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        await _context.Budgets.AddAsync(budget, cancellationToken);
        return budget;
    }

    public async Task<Budget> UpdateAsync(
        Budget budget,
        CancellationToken cancellationToken = default
    )
    {
        _context.Budgets.Update(budget);
        return await Task.FromResult(budget);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var budget = await GetByIdAsync(id, cancellationToken);
        if (budget is not null)
        {
            budget.SoftDelete();
        }
    }
}
