using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

public sealed class AnalyticsService(InventoryDbContext db, AuthorizationService authorization)
{
    public async Task<AnalyticsDto> GetAsync(UserSession session, int? warehouseId = null, decimal aThreshold = .80m, decimal bThreshold = .95m,
        DateTime? asOf = null, CancellationToken ct = default)
    {
        if (aThreshold <= 0 || aThreshold >= bThreshold || bThreshold >= 1) throw new ArgumentException("Ngưỡng ABC phải thỏa 0 < A < B < 100%.");
        await authorization.EnsureActiveActorAsync(session, ct);
        var scope = await authorization.GetAccessibleWarehouseIdsAsync(session, ct);
        if (authorization.IsStaff(session) && scope.Count == 0) throw new UnauthorizedAccessException("Không có phạm vi kho được phép.");
        if (warehouseId.HasValue && !scope.Contains(warehouseId.Value)) throw new UnauthorizedAccessException("Không có quyền xem kho này.");
        var ids = warehouseId.HasValue ? new[] { warehouseId.Value } : scope.ToArray();
        var today = (asOf ?? DateTime.Now).Date;
        if (today.Year == 9999 || today.Year < 2) throw new ArgumentOutOfRangeException(nameof(asOf));
        var end = today.AddDays(1);
        var abcFrom = today.AddMonths(-12);
        var deadFrom = today.AddDays(-89);
        var inventory = await db.Inventories.AsNoTracking().Where(x => ids.Contains(x.WarehouseId))
            .OrderBy(x => x.Product.ProductCode).ThenBy(x => x.Warehouse.WarehouseCode)
            .Select(x => new
            {
                x.Product.ProductCode,
                x.Product.ProductName,
                x.Warehouse.WarehouseCode,
                x.Warehouse.WarehouseName,
                x.Quantity,
                x.Product.PurchasePrice,
                x.Product.ReorderPoint,
                x.Product.MaximumStock,
                x.Product.IsActive,
                First = db.InventoryTransactions.Where(t => t.ProductId == x.ProductId && t.WarehouseId == x.WarehouseId && t.CreatedAt < end).Select(t => (DateTime?)t.CreatedAt).Min(),
                LastExport = db.ExportDetails.Where(d => d.ProductId == x.ProductId && d.ExportReceipt.WarehouseId == x.WarehouseId && d.ExportReceipt.Status == "COMPLETED" && d.ExportReceipt.ReceiptDate < end)
                    .Select(d => (DateTime?)d.ExportReceipt.ReceiptDate).Max()
            }).ToListAsync(ct);
        var stock = inventory.Select(x => new AnalyticsStockDto(x.ProductCode, x.ProductName, x.WarehouseCode, x.WarehouseName, x.Quantity, x.PurchasePrice,
            checked(x.Quantity * x.PurchasePrice), x.IsActive && x.Quantity <= x.ReorderPoint ? Math.Max(0, x.MaximumStock - x.Quantity) : 0,
            x.Quantity == 0 ? "NO_STOCK" : !x.First.HasValue || x.First.Value.Date > deadFrom ? "INSUFFICIENT_HISTORY" : x.LastExport >= deadFrom ? "MOVING" : "DEAD_STOCK",
            x.First, x.LastExport)).ToList();
        var values = await db.ExportDetails.AsNoTracking().Where(d => ids.Contains(d.ExportReceipt.WarehouseId) && d.ExportReceipt.Status == "COMPLETED" && d.ExportReceipt.ReceiptDate >= abcFrom && d.ExportReceipt.ReceiptDate < end)
            .GroupBy(d => new { d.Product.ProductCode, d.Product.ProductName, d.ExportReceipt.Warehouse.WarehouseCode, d.ExportReceipt.Warehouse.WarehouseName })
            .Select(group => new { group.Key.ProductCode, group.Key.ProductName, group.Key.WarehouseCode, group.Key.WarehouseName, Value = group.Sum(d => d.Quantity * d.UnitPrice) })
            .OrderByDescending(x => x.Value).ThenBy(x => x.ProductCode).ThenBy(x => x.WarehouseCode).ToListAsync(ct);
        var total = values.Sum(x => x.Value);
        decimal cumulative = 0;
        var abc = new List<AbcAnalysisDto>();
        foreach (var row in values)
        {
            var group = total == 0 ? "UNCLASSIFIED" : row.Value == 0 ? "C" : cumulative < aThreshold ? "A" : cumulative < bThreshold ? "B" : "C";
            var share = total == 0 ? 0 : row.Value / total;
            cumulative += share;
            abc.Add(new(row.ProductCode, row.ProductName, row.WarehouseCode, row.WarehouseName, row.Value, share, cumulative, group));
        }
        return new(today, abcFrom, deadFrom, stock.Sum(x => x.EstimatedValue), stock, abc);
    }
}
