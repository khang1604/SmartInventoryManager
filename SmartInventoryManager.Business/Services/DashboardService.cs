using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Queries;

namespace SmartInventoryManager.Business.Services;

public sealed class DashboardService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    // Khởi tạo service xử lý các chỉ số tổng quan kho cùng các thành phần phụ thuộc.
    public DashboardService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    // Lấy các chỉ số tổng quan kho theo điều kiện tra cứu và phạm vi truy cập.
    public async Task<DashboardDto> GetAsync(UserSession session, int? warehouseId = null, DateTime? from = null,
        DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Now.Date;
        from ??= today.AddDays(-29);
        to ??= today;
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

        var daily = await new DashboardQueries(_dbContext).GetDailyAsync(warehouseIds, from.Value.Date, to.Value.Date.AddDays(1), cancellationToken);
        var lowest = await inventory.OrderBy(item => item.Quantity).ThenBy(item => item.Product.ProductCode).ThenBy(item => item.Warehouse.WarehouseCode)
            .Take(10).Select(item => new DashboardLowestStockDto(item.Product.ProductCode, item.Warehouse.WarehouseCode, item.Quantity)).ToListAsync(cancellationToken);
        var distribution = new List<DashboardStatusCountDto>
        {
            new("OUT_OF_STOCK", await inventory.CountAsync(item => item.Quantity == 0, cancellationToken)),
            new("LOW_STOCK", await inventory.CountAsync(item => item.Quantity > 0 && item.Quantity <= item.Product.ReorderPoint, cancellationToken)),
            new("NORMAL", await inventory.CountAsync(item => item.Quantity > item.Product.ReorderPoint && item.Quantity <= item.Product.MaximumStock, cancellationToken)),
            new("OVERSTOCK", await inventory.CountAsync(item => item.Quantity > item.Product.MaximumStock, cancellationToken))
        };
        var filledDays = new List<DashboardDailyQuantityDto>();
        for (var date = from.Value.Date; date <= to.Value.Date; date = date.AddDays(1))
        {
            var day = daily.FirstOrDefault(item => item.Date == date);
            filledDays.Add(new(date, day?.ImportQuantity ?? 0, day?.ExportQuantity ?? 0));
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
            await transactions.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.TransactionId).Take(20)
                .Select(item => new DashboardTransactionDto(item.TransactionId, item.Product.ProductCode,
                    item.Product.ProductName, item.Warehouse.WarehouseName, item.TransactionType, item.Quantity,
                    item.CreatedAt)
                {
                    ActorName = item.Creator.FullName,
                    ReferenceCode = item.ReferenceType == "IMPORT_RECEIPT"
                        ? _dbContext.ImportReceipts.Where(receipt => receipt.ImportReceiptId == item.ReferenceId).Select(receipt => receipt.ReceiptCode).FirstOrDefault()
                        : item.ReferenceType == "EXPORT_RECEIPT"
                            ? _dbContext.ExportReceipts.Where(receipt => receipt.ExportReceiptId == item.ReferenceId).Select(receipt => receipt.ReceiptCode).FirstOrDefault() : null
                }).ToListAsync(cancellationToken))
        {
            DailyQuantities = filledDays, LowestStock = lowest, StockDistribution = distribution,
            EstimatedInventoryValue = await inventory.Select(item => item.Quantity * item.Product.PurchasePrice).SumAsync(cancellationToken)
        };
    }
}
