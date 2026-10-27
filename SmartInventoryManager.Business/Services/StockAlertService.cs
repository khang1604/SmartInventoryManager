using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Events;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Data.Context;
using StockAlertEntity = SmartInventoryManager.Data.Entities.StockAlert;

namespace SmartInventoryManager.Business.Services;

public sealed class StockAlertService : IStockAlertService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly StockEventPublisher _publisher;

    // Khởi tạo service xử lý cảnh báo tồn kho cùng các thành phần phụ thuộc.
    public StockAlertService(InventoryDbContext dbContext, AuthorizationService authorization, StockEventPublisher? publisher = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _publisher = publisher ?? new StockEventPublisher();
    }

    public event EventHandler<StockAlertEvent>? AlertChanged
    { add => _publisher.AlertChanged += value; remove => _publisher.AlertChanged -= value; }

    // Lấy các cảnh báo tồn kho chưa được xử lý trong phạm vi cho phép.
    public async Task<IReadOnlyList<StockAlertDto>> GetOpenAsync(
        UserSession session,
        int warehouseId,
        CancellationToken cancellationToken = default)
        => await GetAsync(session, warehouseId, false, cancellationToken);

    // Lấy các cảnh báo tồn kho đã được xử lý trong phạm vi cho phép.
    public Task<IReadOnlyList<StockAlertDto>> GetResolvedAsync(
        UserSession session, int warehouseId, CancellationToken cancellationToken = default)
        => GetAsync(session, warehouseId, true, cancellationToken);

    // Lấy cảnh báo tồn kho theo điều kiện tra cứu và phạm vi truy cập.
    private async Task<IReadOnlyList<StockAlertDto>> GetAsync(
        UserSession session, int warehouseId, bool resolved, CancellationToken cancellationToken)
    {
        ValidateSession(session);
        ValidateWarehouseId(warehouseId);
        if (!await _authorization.CanViewWarehouseAsync(session, warehouseId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem cảnh báo tại kho này.");
        }

        return await _dbContext.StockAlerts
            .AsNoTracking()
            .Where(alert => alert.WarehouseId == warehouseId && alert.IsResolved == resolved)
            .OrderByDescending(alert => alert.CreatedAt)
            .Select(alert => new StockAlertDto(
                alert.AlertId,
                alert.ProductId,
                alert.Product.ProductCode,
                alert.Product.ProductName,
                alert.WarehouseId,
                alert.AlertType,
                alert.CurrentQuantity,
                alert.Threshold,
                alert.IsResolved,
                alert.CreatedAt,
                alert.ResolvedAt)
            {
                ResolvedByName = _dbContext.Users.Where(user => user.UserId == alert.ResolvedBy)
                    .Select(user => user.FullName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    // Đối chiếu số tồn và ngưỡng để tạo, đổi loại hoặc đóng cảnh báo.
    public async Task RefreshAsync(
        int productId,
        int warehouseId,
        CancellationToken cancellationToken = default,
        bool persistChanges = true,
        bool publishEvent = true)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
        if (warehouseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouseId));
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == productId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        // Include pending stock changes/new inventory rows from the same workflow.
        // A scalar database projection would read the quantity before SaveChanges.
        var trackedInventory = _dbContext.ChangeTracker.Entries<SmartInventoryManager.Data.Entities.Inventory>()
            .SingleOrDefault(entry => entry.State is EntityState.Added or EntityState.Modified
                && entry.Entity.ProductId == productId && entry.Entity.WarehouseId == warehouseId);
        var quantity = trackedInventory is not null
            ? trackedInventory.Entity.Quantity
            : await _dbContext.Inventories.AsNoTracking()
                .Where(item => item.ProductId == productId && item.WarehouseId == warehouseId)
                .Select(item => (int?)item.Quantity)
                .SingleOrDefaultAsync(cancellationToken) ?? 0;
        var status = StockStatusCalculator.Calculate(quantity, product.ReorderPoint, product.MaximumStock);
        var alertType = ToAlertType(status);
        var openAlerts = await _dbContext.StockAlerts
            .Where(alert => alert.ProductId == productId && alert.WarehouseId == warehouseId && !alert.IsResolved)
            .ToListAsync(cancellationToken);
        StockAlertEntity? currentAlert = null;

        foreach (var staleAlert in openAlerts.Where(alert => alert.AlertType != alertType || alertType == "NORMAL"))
        {
            staleAlert.IsResolved = true;
            staleAlert.ResolvedAt = DateTime.Now;
        }

        if (alertType != "NORMAL")
        {
            currentAlert = openAlerts.FirstOrDefault(alert => alert.AlertType == alertType);
            if (currentAlert is null)
            {
                currentAlert = new StockAlertEntity
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    AlertType = alertType,
                    CurrentQuantity = quantity,
                    Threshold = status == StockStatus.Overstock ? product.MaximumStock : product.ReorderPoint,
                    IsResolved = false,
                    CreatedAt = DateTime.Now
                };
                _dbContext.StockAlerts.Add(currentAlert);
            }
            else
            {
                currentAlert.CurrentQuantity = quantity;
                currentAlert.Threshold = status == StockStatus.Overstock ? product.MaximumStock : product.ReorderPoint;
            }
        }

        if (persistChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        if (publishEvent)
        {
            _publisher.Publish(new StockAlertEvent(
                productId,
                warehouseId,
                alertType,
                currentAlert?.AlertId ?? 0,
                quantity));
        }
    }

    // Ghi nhận người dùng đã xử lý cảnh báo tồn kho.
    public async Task ResolveAsync(
        UserSession session,
        long alertId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        if (alertId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(alertId));
        }

        var alert = await _dbContext.StockAlerts
            .SingleOrDefaultAsync(item => item.AlertId == alertId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy cảnh báo tồn kho.");
        if (!await _authorization.CanResolveStockAlertAsync(session, alert.WarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xử lý cảnh báo tại kho này.");
        }
        if (alert.IsResolved)
        {
            return;
        }
        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.Now;
        alert.ResolvedBy = session.CurrentUser!.UserId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        _publisher.Publish(new StockAlertEvent(
            alert.ProductId,
            alert.WarehouseId,
            alert.AlertType,
            alert.AlertId,
            alert.CurrentQuantity,
            alert.ResolvedAt ?? DateTime.Now));
    }

    // Đóng các cảnh báo đang mở của sản phẩm theo điều kiện xử lý.
    public async Task ResolveForProductAsync(
        int productId,
        CancellationToken cancellationToken = default,
        bool persistChanges = true,
        bool publishEvent = true)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        var alerts = await _dbContext.StockAlerts
            .Where(alert => alert.ProductId == productId && !alert.IsResolved)
            .ToListAsync(cancellationToken);
        var now = DateTime.Now;
        foreach (var alert in alerts)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = now;
        }

        if (persistChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (publishEvent)
        {
            foreach (var alert in alerts)
            {
                _publisher.Publish(new StockAlertEvent(
                    alert.ProductId,
                    alert.WarehouseId,
                    alert.AlertType,
                    alert.AlertId,
                    alert.CurrentQuantity,
                    alert.ResolvedAt ?? now));
            }
        }
    }

    // Chuyển trạng thái tồn kho sang loại cảnh báo tương ứng.
    private static string ToAlertType(StockStatus status)
    {
        return status switch
        {
            StockStatus.OutOfStock => "OUT_OF_STOCK",
            StockStatus.LowStock => "LOW_STOCK",
            StockStatus.Normal => "NORMAL",
            StockStatus.Overstock => "OVERSTOCK",
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
    }

    // Kiểm tra phiên đăng nhập hợp lệ trước khi xử lý yêu cầu.
    private static void ValidateSession(UserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentUser is null)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xử lý cảnh báo tồn kho.");
        }
    }

    // Kiểm tra mã định danh kho là số dương.
    private static void ValidateWarehouseId(int warehouseId)
    {
        if (warehouseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouseId));
        }
    }
}
