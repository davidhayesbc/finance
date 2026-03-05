using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class TagRepository : ITagRepository
{
    private readonly PrivestioDbContext _context;

    public TagRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default
    ) =>
        await _context
            .Tags.Where(t => t.OwnerId == ownerId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<Tag?> FindByNameAsync(
        string name,
        Guid ownerId,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Tags.FirstOrDefaultAsync(
            t => t.OwnerId == ownerId && t.Name.ToLower() == name.Trim().ToLower(),
            cancellationToken
        );

    public async Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await _context.Tags.AddAsync(tag, cancellationToken);
        return tag;
    }

    public Task<Tag> UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        _context.Tags.Update(tag);
        return Task.FromResult(tag);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await GetByIdAsync(id, cancellationToken);
        if (tag is not null)
        {
            tag.SoftDelete();
        }
    }
}
