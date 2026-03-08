using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class SinkingFundRepository : ISinkingFundRepository
{
    private readonly PrivestioDbContext _context;

    public SinkingFundRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<SinkingFund?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .SinkingFunds.Include(s => s.Account)
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SinkingFund>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .SinkingFunds.Where(s => s.UserId == userId)
            .Include(s => s.Account)
            .Include(s => s.Category)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SinkingFund>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .SinkingFunds.Where(s => s.UserId == userId && s.IsActive)
            .Include(s => s.Account)
            .Include(s => s.Category)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<SinkingFund> AddAsync(
        SinkingFund sinkingFund,
        CancellationToken cancellationToken = default
    )
    {
        await _context.SinkingFunds.AddAsync(sinkingFund, cancellationToken);
        return sinkingFund;
    }

    public async Task<SinkingFund> UpdateAsync(
        SinkingFund sinkingFund,
        CancellationToken cancellationToken = default
    )
    {
        _context.SinkingFunds.Update(sinkingFund);
        return await Task.FromResult(sinkingFund);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sinkingFund = await GetByIdAsync(id, cancellationToken);
        if (sinkingFund is not null)
        {
            sinkingFund.SoftDelete();
        }
    }
}
