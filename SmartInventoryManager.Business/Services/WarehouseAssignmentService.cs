using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed record WarehouseAssignmentDto(int UserId, int WarehouseId, string WarehouseName, bool IsActive);

public sealed class WarehouseAssignmentService(InventoryDbContext db, AuthorizationService authorization)
{
    private async Task RequireAdminAsync(UserSession session, CancellationToken ct)
    {
        if (!authorization.IsAdmin(session)) throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý phân kho.");
        await authorization.EnsureActiveActorAsync(session, ct);
    }

    public async Task<IReadOnlyList<WarehouseAssignmentDto>> GetAsync(UserSession session, int userId, CancellationToken ct = default)
    {
        await RequireAdminAsync(session, ct);
        return await db.UserWarehouses.AsNoTracking().Where(item => item.UserId == userId)
            .OrderBy(item => item.Warehouse.WarehouseName)
            .Select(item => new WarehouseAssignmentDto(item.UserId, item.WarehouseId, item.Warehouse.WarehouseName, item.IsActive))
            .ToListAsync(ct);
    }

    public async Task SetActiveAsync(UserSession session, int userId, int warehouseId, bool active, CancellationToken ct = default)
    {
        await RequireAdminAsync(session, ct);
        if (userId <= 0 || warehouseId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        if (active)
        {
            if (!await db.Users.AnyAsync(user => user.UserId == userId && user.IsActive && user.Role.RoleName == RoleNames.Staff, ct))
                throw new InvalidOperationException("Chỉ phân kho cho tài khoản nhân viên đang hoạt động.");
            if (!await db.Warehouses.AnyAsync(warehouse => warehouse.WarehouseId == warehouseId && warehouse.IsActive, ct))
                throw new InvalidOperationException("Kho phải đang hoạt động.");
        }
        var assignment = await db.UserWarehouses.SingleOrDefaultAsync(item => item.UserId == userId && item.WarehouseId == warehouseId, ct);
        if (assignment is null)
        {
            if (!active) throw new KeyNotFoundException("Không tìm thấy phân kho.");
            assignment = new UserWarehouse { UserId = userId, WarehouseId = warehouseId };
            db.UserWarehouses.Add(assignment);
        }
        assignment.IsActive = active;
        db.SystemLogs.Add(new SystemLog { UserId = session.CurrentUser!.UserId, Action = active ? "ACTIVATE_ASSIGNMENT" : "DEACTIVATE_ASSIGNMENT",
            EntityName = "UserWarehouse", Description = $"Tài khoản #{userId}; kho #{warehouseId}.", CreatedAt = DateTime.Now });
        await db.SaveChangesAsync(ct);
    }
}
