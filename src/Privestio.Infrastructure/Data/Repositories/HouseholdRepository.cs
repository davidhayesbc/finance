using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;

namespace Privestio.Infrastructure.Data.Repositories;

public class HouseholdRepository : IHouseholdRepository
{
    private readonly PrivestioDbContext _context;

    public HouseholdRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Household?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.Households.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<Household?> GetByIdWithMembersAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Households.Include(h => h.Members)
            .ThenInclude(m => m.User)
            .Include(h => h.Invitations)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<Household?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Households.Include(h => h.Members)
            .ThenInclude(m => m.User)
            .Include(h => h.Invitations)
            .FirstOrDefaultAsync(
                h => h.Members.Any(m => m.UserId == userId),
                cancellationToken
            );

    public async Task<HouseholdInvitation?> GetInvitationByTokenAsync(
        Guid token,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .HouseholdInvitations.Include(i => i.Household)
            .ThenInclude(h => h.Members)
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

    public async Task<HouseholdMember?> GetMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context.HouseholdMembers.FirstOrDefaultAsync(
            m => m.HouseholdId == householdId && m.UserId == userId,
            cancellationToken
        );

    public async Task<Household> AddAsync(
        Household household,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Households.AddAsync(household, cancellationToken);
        return household;
    }

    public async Task<Household> UpdateAsync(
        Household household,
        CancellationToken cancellationToken = default
    )
    {
        _context.Households.Update(household);
        return await Task.FromResult(household);
    }

    public async Task<bool> IsUserInAnyHouseholdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context.HouseholdMembers.AnyAsync(
            m => m.UserId == userId,
            cancellationToken
        );

    public async Task<HouseholdRole?> GetMemberRoleAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var member = await _context.HouseholdMembers.FirstOrDefaultAsync(
            m => m.HouseholdId == householdId && m.UserId == userId,
            cancellationToken
        );
        return member?.Role;
    }
}
