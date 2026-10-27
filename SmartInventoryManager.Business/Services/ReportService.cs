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

    // Khởi tạo service báo cáo với DbContext và cơ chế giới hạn phạm vi kho.
    public ReportService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    // Tổng hợp báo cáo tồn kho theo phạm vi kho và các điều kiện lọc.
    public async Task<InventoryReportDto> GetInventoryAsync(UserSession session, int? warehouseId = null,
        CancellationToken cancellationToken = default, string? search = null, string? stockStatus = null)
    {
        var warehouseIds = await GetWarehouseScopeAsync(session, warehouseId, cancellationToken);
        var inventory = _dbContext.Inventories.AsNoTracking()
            .Where(item => warehouseIds.Contains(item.WarehouseId));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            inventory = inventory.Where(item => item.Product.ProductCode.Contains(value) || item.Product.ProductName.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(stockStatus))
            inventory = stockStatus switch
            {
                "OUT_OF_STOCK" => inventory.Where(item => item.Quantity == 0),
                "LOW_STOCK" => inventory.Where(item => item.Quantity > 0 && item.Quantity <= item.Product.ReorderPoint),
                "NORMAL" => inventory.Where(item => item.Quantity > item.Product.ReorderPoint && item.Quantity <= item.Product.MaximumStock),
                "OVERSTOCK" => inventory.Where(item => item.Quantity > item.Product.MaximumStock),
                _ => throw new ArgumentException("Trạng thái tồn không hợp lệ.", nameof(stockStatus))
            };
        var totalQuantity = await inventory.Select(item => (long)item.Quantity).SumAsync(cancellationToken);
        var rows = await inventory
            .OrderBy(item => item.Warehouse.WarehouseName)
            .ThenBy(item => item.Product.ProductCode)
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
                item.UpdatedAt)
            {
                PurchasePrice = item.Product.PurchasePrice, EstimatedValue = item.Quantity * item.Product.PurchasePrice
            })
            .ToListAsync(cancellationToken);
        var openAlertCount = await _dbContext.StockAlerts.AsNoTracking()
            .CountAsync(item => !item.IsResolved && inventory.Any(stock => stock.ProductId == item.ProductId && stock.WarehouseId == item.WarehouseId), cancellationToken);
        return new InventoryReportDto(totalQuantity, openAlertCount, rows) { EstimatedInventoryValue = rows.Sum(row => row.EstimatedValue) };
    }

    // Tổng hợp báo cáo nhập xuất từ phiếu hoàn tất theo điều kiện lọc.
    public async Task<ImportExportReportDto> GetImportExportAsync(UserSession session, int? warehouseId = null,
        DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default,
        string? search = null, string? receiptType = null)
    {
        if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date)
            throw new ArgumentException("Khoảng ngày không hợp lệ.", nameof(to));
        var warehouseIds = await GetWarehouseScopeAsync(session, warehouseId, cancellationToken);
        var imports = _dbContext.ImportReceipts.AsNoTracking()
            .Where(item => warehouseIds.Contains(item.WarehouseId) && item.Status == "COMPLETED");
        var exports = _dbContext.ExportReceipts.AsNoTracking()
            .Where(item => warehouseIds.Contains(item.WarehouseId) && item.Status == "COMPLETED");
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            imports = imports.Where(item => item.ReceiptCode.Contains(value));
            exports = exports.Where(item => item.ReceiptCode.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(receiptType))
        {
            if (receiptType == "IMPORT") exports = exports.Where(item => false);
            else if (receiptType == "EXPORT") imports = imports.Where(item => false);
            else throw new ArgumentException("Loại phiếu không hợp lệ.", nameof(receiptType));
        }
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
        var importTotal = await imports.Select(item => item.TotalAmount).SumAsync(cancellationToken);
        var exportTotal = await exports.Select(item => item.TotalAmount).SumAsync(cancellationToken);
        var importRows = await imports.OrderByDescending(item => item.ReceiptDate).ThenBy(item => item.ImportReceiptId)
            .Select(item => new ImportReceiptListItemDto(item.ImportReceiptId, item.ReceiptCode, item.Supplier.SupplierName,
                item.Warehouse.WarehouseName, item.Creator.FullName, item.ReceiptDate, item.TotalAmount, item.Status))
            .ToListAsync(cancellationToken);
        var exportRows = await exports.OrderByDescending(item => item.ReceiptDate).ThenBy(item => item.ExportReceiptId)
            .Select(item => new ExportReceiptListItemDto(item.ExportReceiptId, item.ReceiptCode,
                item.Warehouse.WarehouseName, item.Creator.FullName, item.ReceiptDate, item.TotalAmount, item.Status))
            .ToListAsync(cancellationToken);
        return new ImportExportReportDto(from, to, warehouseId, importRows, exportRows,
            importTotal, exportTotal);
    }

    // Xác định phạm vi kho hợp lệ cho yêu cầu báo cáo.
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
