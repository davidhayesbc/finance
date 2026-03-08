using Microsoft.EntityFrameworkCore;
using Privestio.Application.Interfaces;
using Privestio.Domain.Entities;

namespace Privestio.Infrastructure.Data.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly PrivestioDbContext _context;

    public NotificationRepository(PrivestioDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        bool includeRead = false,
        int limit = 50,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (!includeRead)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await _context.Notifications.CountAsync(
            n => n.UserId == userId && !n.IsRead,
            cancellationToken
        );

    public async Task<Notification> AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default
    )
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        return notification;
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await GetByIdAsync(id, cancellationToken);
        notification?.MarkAsRead();
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _context
            .Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkAsRead();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await GetByIdAsync(id, cancellationToken);
        if (notification is not null)
        {
            notification.SoftDelete();
        }
    }
}
