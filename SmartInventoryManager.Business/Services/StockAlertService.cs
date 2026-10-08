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

    public StockAlertService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public event EventHandler<StockAlertEvent>? AlertChanged;

    public async Task<IReadOnlyList<StockAlertDto>> GetOpenAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockAlerts
            .AsNoTracking()
            .Where(alert => alert.WarehouseId == warehouseId && !alert.IsResolved)
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
                alert.ResolvedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task RefreshAsync(
        int productId,
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProductId == productId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        var quantity = await _dbContext.Inventories
            .Where(item => item.ProductId == productId && item.WarehouseId == warehouseId)
            .Select(item => (int?)item.Quantity)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;
        var status = StockStatusCalculator.Calculate(quantity, product.ReorderPoint, product.MaximumStock);
        var alertType = ToAlertType(status);
        var openAlerts = await _dbContext.StockAlerts
            .Where(alert => alert.ProductId == productId && alert.WarehouseId == warehouseId && !alert.IsResolved)
            .ToListAsync(cancellationToken);

        foreach (var staleAlert in openAlerts.Where(alert => alert.AlertType != alertType || alertType == "NORMAL"))
        {
            staleAlert.IsResolved = true;
            staleAlert.ResolvedAt = DateTime.Now;
        }

        if (alertType != "NORMAL")
        {
            var currentAlert = openAlerts.FirstOrDefault(alert => alert.AlertType == alertType);
            if (currentAlert is null)
            {
                _dbContext.StockAlerts.Add(new StockAlertEntity
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    AlertType = alertType,
                    CurrentQuantity = quantity,
                    Threshold = status == StockStatus.Overstock ? product.MaximumStock : product.ReorderPoint,
                    IsResolved = false,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                currentAlert.CurrentQuantity = quantity;
                currentAlert.Threshold = status == StockStatus.Overstock ? product.MaximumStock : product.ReorderPoint;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        AlertChanged?.Invoke(this, new StockAlertEvent(productId, warehouseId, alertType));
    }

    public async Task ResolveAsync(
        UserSession session,
        long alertId,
        CancellationToken cancellationToken = default)
    {
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
        AlertChanged?.Invoke(this, new StockAlertEvent(alert.ProductId, alert.WarehouseId, alert.AlertType));
    }

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
}
