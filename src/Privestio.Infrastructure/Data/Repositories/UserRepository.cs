using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PrivestioDbContext _context;

    public UserRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.DomainUsers.Update(user);
        return await Task.FromResult(user);
    }
}