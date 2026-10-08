using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class DashboardService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    public DashboardService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<DashboardDto> GetAsync(UserSession session, int? warehouseId = null, DateTime? from = null,
        DateTime? to = null, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem tổng quan.");
        if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date)
            throw new ArgumentException("Khoảng ngày không hợp lệ.", nameof(to));

        var accessible = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        if (warehouseId.HasValue && !accessible.Contains(warehouseId.Value))
            throw new UnauthorizedAccessException("Bạn không có quyền xem dữ liệu tại kho này.");
        var warehouseIds = warehouseId.HasValue ? new[] { warehouseId.Value } : accessible.ToArray();
        var inventory = _dbContext.Inventories.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId));
        var alerts = _dbContext.StockAlerts.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId) && !item.IsResolved);
        var imports = _dbContext.ImportReceipts.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId) && item.Status == "COMPLETED");
        var exports = _dbContext.ExportReceipts.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId) && item.Status == "COMPLETED");
        var transactions = _dbContext.InventoryTransactions.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId));
        if (from.HasValue)
        {
            imports = imports.Where(item => item.ReceiptDate >= from.Value.Date);
            exports = exports.Where(item => item.ReceiptDate >= from.Value.Date);
            transactions = transactions.Where(item => item.CreatedAt >= from.Value.Date);
        }
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            imports = imports.Where(item => item.ReceiptDate < end);
            exports = exports.Where(item => item.ReceiptDate < end);
            transactions = transactions.Where(item => item.CreatedAt < end);
        }

        return new DashboardDto(
            warehouseId, from, to,
            await inventory.CountAsync(cancellationToken),
            await inventory.Select(item => (long)item.Quantity).SumAsync(cancellationToken),
            await inventory.CountAsync(item => item.Quantity == 0, cancellationToken),
            await alerts.CountAsync(cancellationToken),
            await imports.CountAsync(cancellationToken),
            await imports.Select(item => item.TotalAmount).SumAsync(cancellationToken),
            await exports.CountAsync(cancellationToken),
            await exports.Select(item => item.TotalAmount).SumAsync(cancellationToken),
            await transactions.OrderByDescending(item => item.CreatedAt).Take(10)
                .Select(item => new DashboardTransactionDto(item.TransactionId, item.Product.ProductCode,
                    item.Product.ProductName, item.Warehouse.WarehouseName, item.TransactionType, item.Quantity,
                    item.CreatedAt)).ToListAsync(cancellationToken));
    }
}
