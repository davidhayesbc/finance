using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class ContributionRoomRepository : IContributionRoomRepository
{
    private readonly PrivestioDbContext _context;

    public ContributionRoomRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<ContributionRoom?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ContributionRooms.Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ContributionRoom>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ContributionRooms.Where(c => c.AccountId == accountId)
            .Include(c => c.Account)
            .OrderByDescending(c => c.Year)
            .ToListAsync(cancellationToken);

    public async Task<ContributionRoom?> GetByAccountIdAndYearAsync(
        Guid accountId,
        int year,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .ContributionRooms.Include(c => c.Account)
            .FirstOrDefaultAsync(
                c => c.AccountId == accountId && c.Year == year,
                cancellationToken
            );

    public async Task<ContributionRoom> AddAsync(
        ContributionRoom contributionRoom,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ContributionRooms.AddAsync(contributionRoom, cancellationToken);
        return contributionRoom;
    }

    public async Task<ContributionRoom> UpdateAsync(
        ContributionRoom contributionRoom,
        CancellationToken cancellationToken = default
    )
    {
        _context.ContributionRooms.Update(contributionRoom);
        return await Task.FromResult(contributionRoom);
    }
}
