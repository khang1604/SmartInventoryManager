using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class NotificationService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    // Khởi tạo service xử lý thông báo cùng các thành phần phụ thuộc.
    public NotificationService(InventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = new AuthorizationService(_dbContext);
    }

    // Lấy thông báo theo điều kiện tra cứu và phạm vi truy cập.
    public async Task<IReadOnlyList<NotificationDto>> GetAsync(UserSession session, string? search = null, bool? isRead = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = await QueryListAsync(session, search, isRead, from, to, cancellationToken);
        return await query.ToListAsync(cancellationToken);
    }

    // Lấy một trang thông báo theo bộ lọc và phạm vi truy cập.
    public async Task<PagedResultDto<NotificationDto>> GetPageAsync(UserSession session, int pageNumber = 1, int pageSize = 50, string? search = null, bool? isRead = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = await QueryListAsync(session, search, isRead, from, to, cancellationToken);
        return await QueryPaging.ReadAsync(query, pageNumber, pageSize, cancellationToken);
    }

    // Tạo truy vấn thông báo với các điều kiện lọc và giới hạn quyền.
    private async Task<IQueryable<NotificationDto>> QueryListAsync(UserSession session, string? search, bool? isRead, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var userId = RequireUserId(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date)
        {
            throw new ArgumentException("Khoảng ngày không hợp lệ.", nameof(to));
        }
        var query = _dbContext.Notifications.AsNoTracking().Where(item => item.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item => item.Title.Contains(value) || item.Message.Contains(value));
        }
        if (isRead.HasValue) query = query.Where(item => item.IsRead == isRead.Value);
        if (from.HasValue) query = query.Where(item => item.CreatedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(item => item.CreatedAt < to.Value.Date.AddDays(1));
        return query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.NotificationId)
            .Select(item => new NotificationDto(item.NotificationId, item.Title, item.Message,
                item.NotificationType, item.IsRead, item.CreatedAt))
            ;
    }

    // Đánh dấu thông báo thuộc tài khoản hiện tại là đã đọc.
    public async Task MarkAsReadAsync(UserSession session, long notificationId, CancellationToken cancellationToken = default)
    {
        if (notificationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(notificationId));
        }
        var userId = RequireUserId(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        var notification = await _dbContext.Notifications.SingleOrDefaultAsync(
            item => item.NotificationId == notificationId && item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy thông báo.");
        if (notification.IsRead) return;
        notification.IsRead = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Đánh dấu toàn bộ thông báo của tài khoản hiện tại là đã đọc.
    public async Task MarkAllAsReadAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        var notifications = await _dbContext.Notifications
            .Where(item => item.UserId == userId && !item.IsRead).ToListAsync(cancellationToken);
        if (notifications.Count == 0) return;
        foreach (var notification in notifications) notification.IsRead = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Lấy mã người dùng từ phiên đăng nhập hợp lệ.
    private static int RequireUserId(UserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.CurrentUser?.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem thông báo.");
    }
}
