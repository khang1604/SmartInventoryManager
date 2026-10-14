using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartInventoryManager.Business.Events;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

internal static class StockWorkflowEffects
{
    internal static async Task CommitAsync(InventoryDbContext db, IDbContextTransaction? transaction, UserSession session,
        string action, string entityName, int? entityId, StockEventPublisher publisher, Action onCommitted, CancellationToken ct)
    {
        var changed = db.ChangeTracker.Entries<StockAlert>().Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity).ToArray();
        db.SystemLogs.Add(new SystemLog { UserId = session.CurrentUser!.UserId, Action = action, EntityName = entityName,
            EntityId = entityId, Description = "Đã hoàn tất nghiệp vụ kho.", CreatedAt = DateTime.Now });
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        onCommitted();
        foreach (var alert in changed)
            publisher.Publish(new StockAlertEvent(alert.ProductId, alert.WarehouseId, alert.IsResolved ? "NORMAL" : alert.AlertType,
                alert.AlertId, alert.CurrentQuantity));
        // A notification failure after commit must never replay the stock transaction.
        if (!db.Database.IsRelational()) return;
        try
        {
            await using var notifications = new InventoryDbContext(db.OptionsForNotifications());
            foreach (var alert in changed.Where(item => !item.IsResolved))
            {
                var managerId = await notifications.Warehouses.Where(item => item.WarehouseId == alert.WarehouseId).Select(item => item.ManagerId).SingleAsync(ct);
                var recipients = await notifications.Users.Where(user => user.IsActive &&
                    (user.Role.RoleName == RoleNames.Admin || (user.UserId == managerId && user.Role.RoleName == RoleNames.WarehouseManager)))
                    .Select(user => user.UserId).ToListAsync(ct);
                var productName = await notifications.Products.Where(item => item.ProductId == alert.ProductId).Select(item => item.ProductName).SingleAsync(ct);
                var warehouseName = await notifications.Warehouses.Where(item => item.WarehouseId == alert.WarehouseId).Select(item => item.WarehouseName).SingleAsync(ct);
                foreach (var userId in recipients)
                    notifications.Notifications.Add(new Notification { UserId = userId, Title = "Cảnh báo tồn kho", NotificationType = "STOCK_ALERT",
                        Message = $"{productName[..Math.Min(150, productName.Length)]}, {warehouseName[..Math.Min(150, warehouseName.Length)]}: số lượng {alert.CurrentQuantity}.", CreatedAt = DateTime.Now });
            }
            await notifications.SaveChangesAsync(ct);
        }
        catch (Exception ex) { Debug.WriteLine($"Best-effort stock notification failed: {ex.GetType().Name}"); }
    }

    private static DbContextOptions<InventoryDbContext> OptionsForNotifications(this InventoryDbContext db) =>
        new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(db.Database.GetConnectionString()).Options;
}
