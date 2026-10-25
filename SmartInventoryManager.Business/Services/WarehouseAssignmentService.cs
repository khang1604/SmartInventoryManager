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
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
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
        if (transaction is not null) await transaction.CommitAsync(ct);
    }

    public async Task SetWarehousesAsync(UserSession session, int userId, IReadOnlyCollection<int> warehouseIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        if (userId <= 0 || warehouseIds.Any(id => id <= 0)) throw new ArgumentOutOfRangeException(nameof(userId));
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
        await RequireAdminAsync(session, ct);
        if (!await db.Users.AnyAsync(user => user.UserId == userId && user.IsActive && user.Role.RoleName == RoleNames.Staff, ct))
            throw new InvalidOperationException("Chỉ phân kho cho tài khoản nhân viên đang hoạt động.");
        var selected = warehouseIds.ToHashSet();
        var assignments = await db.UserWarehouses.Where(item => item.UserId == userId).ToListAsync(ct);
        // Phân công cũ tại kho ngừng hoạt động có thể giữ lại hoặc thu hồi, không được cấp mới.
        var activated = selected.Except(assignments.Where(item => item.IsActive).Select(item => item.WarehouseId)).ToArray();
        if (await db.Warehouses.CountAsync(warehouse => activated.Contains(warehouse.WarehouseId) && warehouse.IsActive, ct) != activated.Length)
            throw new InvalidOperationException("Kho được phân công thêm phải đang hoạt động.");
        foreach (var id in activated.Where(id => assignments.All(item => item.WarehouseId != id)))
        {
            var added = new UserWarehouse { UserId = userId, WarehouseId = id, IsActive = false };
            db.UserWarehouses.Add(added);
            assignments.Add(added);
        }
        foreach (var item in assignments)
        {
            var active = selected.Contains(item.WarehouseId);
            if (item.IsActive == active) continue;
            item.IsActive = active;
            db.SystemLogs.Add(new SystemLog { UserId = session.CurrentUser!.UserId,
                Action = active ? "ACTIVATE_ASSIGNMENT" : "DEACTIVATE_ASSIGNMENT", EntityName = "UserWarehouse",
                Description = $"Tài khoản #{userId}; kho #{item.WarehouseId}.", CreatedAt = DateTime.Now });
        }
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
    }
}
