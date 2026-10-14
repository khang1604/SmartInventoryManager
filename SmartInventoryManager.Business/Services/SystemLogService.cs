using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed record SystemLogDto(long LogId, string? ActorName, string Action, string? EntityName, string? Description, DateTime CreatedAt);

public sealed class SystemLogService(InventoryDbContext db, AuthorizationService authorization)
{
    public async Task<IReadOnlyList<SystemLogDto>> GetAsync(UserSession session, string? search = null, CancellationToken ct = default)
    {
        if (!authorization.IsAdmin(session)) throw new UnauthorizedAccessException("Chỉ quản trị viên được xem nhật ký hệ thống.");
        await authorization.EnsureActiveActorAsync(session, ct);
        var query = db.SystemLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item => item.Action.Contains(value) || (item.Description != null && item.Description.Contains(value)));
        }
        return await query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.LogId).Take(500)
            .Select(item => new SystemLogDto(item.LogId, item.User == null ? null : item.User.FullName, item.Action, item.EntityName, item.Description, item.CreatedAt)).ToListAsync(ct);
    }
}
