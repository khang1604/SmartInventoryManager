using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class ReportService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    public ReportService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<InventoryReportDto> GetInventoryAsync(UserSession session, int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var warehouseIds = await GetWarehouseScopeAsync(session, warehouseId, cancellationToken);
        var rows = await _dbContext.Inventories.AsNoTracking()
            .Where(item => warehouseIds.Contains(item.WarehouseId))
            .OrderBy(item => item.Warehouse.WarehouseName)
            .ThenBy(item => item.Product.ProductCode)
            .Take(5000)
            .Select(item => new InventoryReportRowDto(
                item.ProductId,
                item.Product.ProductCode,
                item.Product.ProductName,
                item.Product.Unit,
                item.Warehouse.WarehouseName,
                item.Quantity,
                item.Product.ReorderPoint,
                item.Product.MaximumStock,
                StockStatusCalculator.Calculate(item.Quantity, item.Product.ReorderPoint, item.Product.MaximumStock).ToString(),
                item.UpdatedAt))
            .ToListAsync(cancellationToken);
        var openAlertCount = await _dbContext.StockAlerts.AsNoTracking()
            .CountAsync(item => warehouseIds.Contains(item.WarehouseId) && !item.IsResolved, cancellationToken);
        return new InventoryReportDto(rows.Sum(item => (long)item.Quantity), openAlertCount, rows);
    }

    public async Task<ImportExportReportDto> GetImportExportAsync(UserSession session, int? warehouseId = null,
        DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date)
            throw new ArgumentException("Khoảng ngày không hợp lệ.", nameof(to));
        var warehouseIds = await GetWarehouseScopeAsync(session, warehouseId, cancellationToken);
        var imports = _dbContext.ImportReceipts.AsNoTracking()
            .Where(item => warehouseIds.Contains(item.WarehouseId) && item.Status == "COMPLETED");
        var exports = _dbContext.ExportReceipts.AsNoTracking()
            .Where(item => warehouseIds.Contains(item.WarehouseId) && item.Status == "COMPLETED");
        if (from.HasValue)
        {
            imports = imports.Where(item => item.ReceiptDate >= from.Value.Date);
            exports = exports.Where(item => item.ReceiptDate >= from.Value.Date);
        }
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            imports = imports.Where(item => item.ReceiptDate < end);
            exports = exports.Where(item => item.ReceiptDate < end);
        }
        var importRows = await imports.OrderByDescending(item => item.ReceiptDate).Take(5000)
            .Select(item => new ImportReceiptListItemDto(item.ImportReceiptId, item.ReceiptCode, item.Supplier.SupplierName,
                item.Warehouse.WarehouseName, item.Creator.FullName, item.ReceiptDate, item.TotalAmount, item.Status))
            .ToListAsync(cancellationToken);
        var exportRows = await exports.OrderByDescending(item => item.ReceiptDate).Take(5000)
            .Select(item => new ExportReceiptListItemDto(item.ExportReceiptId, item.ReceiptCode,
                item.Warehouse.WarehouseName, item.Creator.FullName, item.ReceiptDate, item.TotalAmount, item.Status))
            .ToListAsync(cancellationToken);
        return new ImportExportReportDto(from, to, warehouseId, importRows, exportRows,
            importRows.Sum(item => item.TotalAmount), exportRows.Sum(item => item.TotalAmount));
    }

    private async Task<int[]> GetWarehouseScopeAsync(UserSession session, int? warehouseId, CancellationToken cancellationToken)
    {
        if (!session.IsAuthenticated)
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem báo cáo.");
        var accessible = await _authorization.GetAccessibleWarehouseIdsAsync(session, cancellationToken);
        if (warehouseId.HasValue && !accessible.Contains(warehouseId.Value))
            throw new UnauthorizedAccessException("Bạn không có quyền xem báo cáo tại kho này.");
        return warehouseId.HasValue ? new[] { warehouseId.Value } : accessible.ToArray();
    }
}
