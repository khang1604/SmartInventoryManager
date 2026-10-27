using SmartInventoryManager.Business.DTOs;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed record SystemLogDto(long LogId, string? ActorName, string Action, string? EntityName, string? Description, DateTime CreatedAt);

public sealed class SystemLogService(InventoryDbContext db, AuthorizationService authorization)
{
    // Lấy nhật ký hệ thống theo điều kiện tra cứu và phạm vi truy cập.
    public async Task<IReadOnlyList<SystemLogDto>> GetAsync(UserSession session, string? search = null, CancellationToken ct = default)
    {
        var query = await QueryListAsync(session, search, ct);
        return await query.ToListAsync(ct);
    }

    // Lấy một trang nhật ký hệ thống theo bộ lọc và phạm vi truy cập.
    public async Task<PagedResultDto<SystemLogDto>> GetPageAsync(UserSession session, int pageNumber = 1, int pageSize = 50, string? search = null, CancellationToken ct = default)
    {
        var query = await QueryListAsync(session, search, ct);
        return await QueryPaging.ReadAsync(query, pageNumber, pageSize, ct);
    }

    // Tạo truy vấn nhật ký hệ thống với các điều kiện lọc và giới hạn quyền.
    private async Task<IQueryable<SystemLogDto>> QueryListAsync(UserSession session, string? search, CancellationToken ct)
    {
        if (!authorization.IsAdmin(session)) throw new UnauthorizedAccessException("Chỉ quản trị viên được xem nhật ký hệ thống.");
        await authorization.EnsureActiveActorAsync(session, ct);
        var query = db.SystemLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item => item.Action.Contains(value) || (item.Description != null && item.Description.Contains(value)));
        }
        return query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.LogId)
            .Select(item => new SystemLogDto(item.LogId, item.User == null ? null : item.User.FullName, item.Action, item.EntityName, item.Description, item.CreatedAt));
    }

}
