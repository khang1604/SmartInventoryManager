using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

internal static class SqlTeamDecisionFixture
{
    public static async Task RunAsync(DbContextOptions<InventoryDbContext> options, UserSession actor)
    {
        await using var db = new InventoryDbContext(options);
        var token = "TDTEST_" + Guid.NewGuid().ToString("N")[..8];
        var category = new Category { CategoryName = token, IsActive = true };
        var a = new Warehouse { WarehouseCode = token + "A", WarehouseName = token + " A", IsActive = true };
        var b = new Warehouse { WarehouseCode = token + "B", WarehouseName = token + " B", IsActive = true };
        db.AddRange(category, a, b); await db.SaveChangesAsync();
        var product = new Product { CategoryId = category.CategoryId, ProductCode = token, ProductName = token, Unit = "piece", ReorderPoint = 2, MaximumStock = 20, PurchasePrice = 3, IsActive = true };
        try
        {
            db.Products.Add(product); await db.SaveChangesAsync();
            db.Inventories.AddRange(new Inventory { ProductId = product.ProductId, WarehouseId = a.WarehouseId, Quantity = 10 }, new Inventory { ProductId = product.ProductId, WarehouseId = b.WarehouseId, Quantity = 10 }); await db.SaveChangesAsync();
            async Task Transfer(int source, int destination)
            {
                await using var ctx = new InventoryDbContext(options);
                await new InventoryService(ctx, new AuthorizationService(ctx)).TransferAsync(actor, source, destination, product.ProductId, 4, token);
            }
            await Task.WhenAll(Transfer(a.WarehouseId, b.WarehouseId), Transfer(b.WarehouseId, a.WarehouseId));
            db.ChangeTracker.Clear();
            var movements = await db.InventoryTransactions.Where(x => x.ProductId == product.ProductId).ToListAsync();
            Assert.AreEqual(4, movements.Count);
            Assert.AreEqual(2, movements.Select(x => x.ReferenceId).Distinct().Count());
            foreach (var pair in movements.GroupBy(x => x.ReferenceId))
            {
                Assert.IsNotNull(pair.Key); Assert.AreEqual(2, pair.Count()); Assert.AreEqual(0, pair.Sum(x => x.SignedDifference));
                Assert.IsTrue(pair.All(x => x.AfterQuantity - x.BeforeQuantity == x.SignedDifference));
            }
            Assert.IsTrue(await db.Inventories.Where(x => x.ProductId == product.ProductId).AllAsync(x => x.Quantity == 10));

            async Task<int> ApprovedExport(string suffix, int quantity)
            {
                await using var ctx = new InventoryDbContext(options); var service = new ExportService(ctx, new AuthorizationService(ctx));
                var id = await service.CreateDraftAsync(actor, new ExportReceiptDto { ReceiptCode = token + suffix, WarehouseId = a.WarehouseId, Details = [new(product.ProductId, quantity, 5)] });
                await service.SubmitAsync(actor, id); await service.ApproveAsync(actor, id); return id;
            }
            var first = await ApprovedExport("E1", 7); var second = await ApprovedExport("E2", 7);
            async Task<bool> Complete(int id)
            {
                await using var ctx = new InventoryDbContext(options);
                try { await new ExportService(ctx, new AuthorizationService(ctx)).CompleteAsync(actor, id); return true; }
                catch (InvalidOperationException ex) when (ex.Message.Contains("tồn") || ex.Message.Contains("hoàn tất") || ex.Message.Contains("trạng thái")) { return false; }
            }
            var results = await Task.WhenAll(Complete(first), Complete(second)); Assert.AreEqual(1, results.Count(x => x));
            db.ChangeTracker.Clear(); Assert.AreEqual(3, (await db.Inventories.SingleAsync(x => x.ProductId == product.ProductId && x.WarehouseId == a.WarehouseId)).Quantity);
            var analytics = await new AnalyticsService(db, new AuthorizationService(db)).GetAsync(actor, a.WarehouseId);
            Assert.AreEqual(9m, analytics.EstimatedInventoryValue); Assert.AreEqual(35m, analytics.Abc.Single().ExportValue);

            var raced = await ApprovedExport("RACE", 1);
            async Task<bool> Cancel()
            {
                await using var ctx = new InventoryDbContext(options);
                try { await new ExportService(ctx, new AuthorizationService(ctx)).CancelAsync(actor, raced, "SQL cancel race"); return true; }
                catch (InvalidOperationException) { return false; }
            }
            await Task.WhenAll(Complete(raced), Cancel()); db.ChangeTracker.Clear();
            var receipt = await db.ExportReceipts.SingleAsync(x => x.ExportReceiptId == raced);
            var count = await db.InventoryTransactions.CountAsync(x => x.ReferenceType == "EXPORT_RECEIPT" && x.ReferenceId == raced);
            Assert.IsTrue((receipt.Status == "COMPLETED" && count == 1 && receipt.CompletedBy.HasValue) || (receipt.Status == "CANCELLED" && count == 0 && receipt.CancelReason == "SQL cancel race"));

            var injected = await ApprovedExport("FAIL", 1);
            var before = (await db.Inventories.SingleAsync(x => x.ProductId == product.ProductId && x.WarehouseId == a.WarehouseId)).Quantity;
            var injectedOptions = new DbContextOptionsBuilder<InventoryDbContext>(options).AddInterceptors(new FailLogWrite()).Options;
            await using (var failing = new InventoryDbContext(injectedOptions))
            {
                var error = await Assert.ThrowsExceptionAsync<DbUpdateException>(() => new ExportService(failing, new AuthorizationService(failing)).CompleteAsync(actor, injected));
                Assert.IsTrue(error.GetBaseException().Message.Contains("Injected failure", StringComparison.Ordinal));
            }
            db.ChangeTracker.Clear();
            Assert.AreEqual(before, (await db.Inventories.SingleAsync(x => x.ProductId == product.ProductId && x.WarehouseId == a.WarehouseId)).Quantity);
            var unchanged = await db.ExportReceipts.SingleAsync(x => x.ExportReceiptId == injected);
            Assert.AreEqual("APPROVED", unchanged.Status); Assert.IsNull(unchanged.CompletedBy); Assert.IsNull(unchanged.CompletedAt);
            Assert.IsFalse(await db.InventoryTransactions.AnyAsync(x => x.ReferenceType == "EXPORT_RECEIPT" && x.ReferenceId == injected));
            Assert.IsFalse(await db.SystemLogs.AnyAsync(x => x.EntityName == "ExportReceipt" && x.EntityId == injected && x.Action == "COMPLETE_EXPORT"));
            Assert.IsFalse(await db.ReceiptStatusChanges.AnyAsync(x => x.ExportReceiptId == injected && x.ToStatus == "COMPLETED"));
        }
        finally
        {
            db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync();
            var warehouses = new[] { a.WarehouseId, b.WarehouseId };
            var exports = await db.ExportReceipts.Where(x => warehouses.Contains(x.WarehouseId)).Select(x => x.ExportReceiptId).ToArrayAsync();
            await db.ReceiptRejections.Where(x => exports.Contains(x.ExportReceiptId ?? 0)).ExecuteDeleteAsync();
            await db.ReceiptStatusChanges.Where(x => exports.Contains(x.ExportReceiptId ?? 0)).ExecuteDeleteAsync();
            await db.SystemLogs.Where(x => (x.EntityName == "Product" && x.EntityId == product.ProductId) || (x.EntityName == "ExportReceipt" && exports.Contains(x.EntityId ?? 0))).ExecuteDeleteAsync();
            await db.Notifications.Where(x => x.Message.Contains(token)).ExecuteDeleteAsync();
            await db.ExportDetails.Where(x => exports.Contains(x.ExportReceiptId)).ExecuteDeleteAsync();
            await db.ExportReceipts.Where(x => exports.Contains(x.ExportReceiptId)).ExecuteDeleteAsync();
            await db.InventoryTransactions.Where(x => warehouses.Contains(x.WarehouseId)).ExecuteDeleteAsync();
            await db.StockAlerts.Where(x => warehouses.Contains(x.WarehouseId)).ExecuteDeleteAsync();
            await db.Inventories.Where(x => warehouses.Contains(x.WarehouseId)).ExecuteDeleteAsync();
            await db.Products.Where(x => x.ProductId == product.ProductId && x.ProductCode == token).ExecuteDeleteAsync();
            await db.Warehouses.Where(x => warehouses.Contains(x.WarehouseId)).ExecuteDeleteAsync();
            await db.Categories.Where(x => x.CategoryId == category.CategoryId && x.CategoryName == token).ExecuteDeleteAsync();
            await tx.CommitAsync();
        }
    }

    private sealed class FailLogWrite : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [SystemLogs]", StringComparison.Ordinal)) throw new InvalidOperationException("Injected failure before log insert");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
