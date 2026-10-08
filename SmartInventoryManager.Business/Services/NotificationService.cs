using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class NotificationService
{
    private readonly InventoryDbContext _dbContext;

    public NotificationService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAsync(
        UserSession session,
        string? search = null,
        bool? isRead = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(session);
        var query = _dbContext.Notifications.AsNoTracking().Where(item => item.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item => item.Title.Contains(value) || item.Message.Contains(value));
        }
        if (isRead.HasValue) query = query.Where(item => item.IsRead == isRead.Value);
        if (from.HasValue) query = query.Where(item => item.CreatedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(item => item.CreatedAt < to.Value.Date.AddDays(1));
        return await query.OrderByDescending(item => item.CreatedAt).Take(500)
            .Select(item => new NotificationDto(item.NotificationId, item.Title, item.Message,
                item.NotificationType, item.IsRead, item.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(UserSession session, long notificationId, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(session);
        var notification = await _dbContext.Notifications.SingleOrDefaultAsync(
            item => item.NotificationId == notificationId && item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy thông báo.");
        if (notification.IsRead) return;
        notification.IsRead = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(session);
        var notifications = await _dbContext.Notifications
            .Where(item => item.UserId == userId && !item.IsRead).ToListAsync(cancellationToken);
        if (notifications.Count == 0) return;
        foreach (var notification in notifications) notification.IsRead = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static int RequireUserId(UserSession session)
    {
        return session.CurrentUser?.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem thông báo.");
    }
}
