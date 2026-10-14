using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

internal static class SqlReceiptFixture
{
    public static async Task RunAsync(DbContextOptions<InventoryDbContext> options, UserSession session)
    {
        await using var setup = new InventoryDbContext(options);
        var token = "WFTEST_" + Guid.NewGuid().ToString("N")[..12];
        var category = new Category { CategoryName = token, IsActive = true };
        var supplier = new Supplier { SupplierCode = token, SupplierName = token, IsActive = true };
        var warehouse = new Warehouse { WarehouseCode = token, WarehouseName = token, IsActive = true };
        setup.AddRange(category, supplier, warehouse); await setup.SaveChangesAsync();
        var product = new Product { ProductCode = token, ProductName = token, CategoryId = category.CategoryId, Unit = "piece", ReorderPoint = 2, MaximumStock = 10, IsActive = true };
        try
        {
            setup.Products.Add(product); await setup.SaveChangesAsync();
            int importId;
            await using (var create = new InventoryDbContext(options))
            {
                importId = await new ImportService(create, new AuthorizationService(create)).CreateDraftAsync(session,
                    new ImportReceiptDto { ReceiptCode = token + "I", WarehouseId = warehouse.WarehouseId, SupplierId = supplier.SupplierId,
                        Details = [new ReceiptDetailDto(product.ProductId, 5, 2m)] });
            }
            await using (var fresh = new InventoryDbContext(options))
            {
                var service = new ImportService(fresh, new AuthorizationService(fresh));
                await service.SubmitAsync(session, importId);
                fresh.ChangeTracker.Clear();
                await service.ApproveAsync(session, importId);
                fresh.ChangeTracker.Clear();
                await service.CompleteAsync(session, importId);
            }
            await using (var verify = new InventoryDbContext(options))
            {
                Assert((await verify.Inventories.SingleAsync(item => item.ProductId == product.ProductId)).Quantity == 5, "fresh-context import Submit/Approve/Complete posts 5");
                Assert(await verify.InventoryTransactions.CountAsync(item => item.ProductId == product.ProductId) == 1, "import posts one movement");
            }
            int exportId;
            await using (var create = new InventoryDbContext(options))
            {
                var service = new ExportService(create, new AuthorizationService(create));
                exportId = await service.CreateDraftAsync(session, new ExportReceiptDto { ReceiptCode = token + "E", WarehouseId = warehouse.WarehouseId,
                    Details = [new ReceiptDetailDto(product.ProductId, 4, 3m)] });
                create.ChangeTracker.Clear();
                await service.SubmitAsync(session, exportId);
                create.ChangeTracker.Clear();
                await service.ApproveAsync(session, exportId);
            }
            var attempts = await Task.WhenAll(CompleteExportAsync(options, session, exportId), CompleteExportAsync(options, session, exportId));
            Assert(attempts.Count(item => item == "completed") == 1, "two contexts completing same export: exactly one succeeds");
            Console.WriteLine("Duplicate completion result: " + string.Join(",", attempts));
            await using (var verify = new InventoryDbContext(options))
            {
                Assert((await verify.Inventories.SingleAsync(item => item.ProductId == product.ProductId)).Quantity == 1, "duplicate completion decrements stock exactly once");
                Assert(await verify.InventoryTransactions.CountAsync(item => item.ProductId == product.ProductId) == 2, "one import and one export movement");
                Assert(await verify.SystemLogs.AnyAsync(item => item.EntityName == "ExportReceipt" && item.EntityId == exportId && item.Action == "COMPLETE_EXPORT"), "completion log persisted with stock");
                Assert(await verify.Notifications.AnyAsync(item => item.Message.Contains(token)), "abnormal alert produces best-effort notification");
                var service = new ExportService(verify, new AuthorizationService(verify));
                var insufficient = await service.CreateDraftAsync(session, new ExportReceiptDto { ReceiptCode = token + "X", WarehouseId = warehouse.WarehouseId,
                    Details = [new ReceiptDetailDto(product.ProductId, 4, 1m)] });
                verify.ChangeTracker.Clear(); await service.SubmitAsync(session, insufficient);
                verify.ChangeTracker.Clear(); await service.ApproveAsync(session, insufficient);
                var rejected = false;
                try { await service.CompleteAsync(session, insufficient); }
                catch (InvalidOperationException) { rejected = true; }
                Assert(rejected, "insufficient export rejected");
                verify.ChangeTracker.Clear();
                Assert((await verify.Inventories.SingleAsync(item => item.ProductId == product.ProductId)).Quantity == 1 &&
                    await verify.InventoryTransactions.CountAsync(item => item.ProductId == product.ProductId) == 2 &&
                    (await verify.ExportReceipts.SingleAsync(item => item.ExportReceiptId == insufficient)).Status == "APPROVED", "failed export leaves stock/movements/receipt unchanged");
            }
        }
        finally
        {
            setup.ChangeTracker.Clear();
            await using var cleanup = await setup.Database.BeginTransactionAsync();
            var importIds = await setup.ImportReceipts.Where(item => item.WarehouseId == warehouse.WarehouseId).Select(item => item.ImportReceiptId).ToArrayAsync();
            var exportIds = await setup.ExportReceipts.Where(item => item.WarehouseId == warehouse.WarehouseId).Select(item => item.ExportReceiptId).ToArrayAsync();
            await setup.SystemLogs.Where(item => (item.EntityName == "ImportReceipt" && importIds.Contains(item.EntityId ?? 0)) ||
                (item.EntityName == "ExportReceipt" && exportIds.Contains(item.EntityId ?? 0)) || (product.ProductId > 0 && item.EntityName == "Product" && item.EntityId == product.ProductId)).ExecuteDeleteAsync();
            await setup.Notifications.Where(item => item.Message.Contains(token)).ExecuteDeleteAsync();
            await setup.ImportDetails.Where(item => item.ImportReceipt.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await setup.ExportDetails.Where(item => item.ExportReceipt.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await setup.InventoryTransactions.Where(item => item.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await setup.StockAlerts.Where(item => item.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await setup.ImportReceipts.Where(item => item.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await setup.ExportReceipts.Where(item => item.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await setup.Inventories.Where(item => item.WarehouseId == warehouse.WarehouseId).ExecuteDeleteAsync();
            await setup.Products.Where(item => item.ProductCode == token && item.CategoryId == category.CategoryId).ExecuteDeleteAsync();
            await setup.Warehouses.Where(item => item.WarehouseId == warehouse.WarehouseId && item.WarehouseCode == token).ExecuteDeleteAsync();
            await setup.Suppliers.Where(item => item.SupplierId == supplier.SupplierId && item.SupplierCode == token).ExecuteDeleteAsync();
            await setup.Categories.Where(item => item.CategoryId == category.CategoryId && item.CategoryName == token).ExecuteDeleteAsync();
            await cleanup.CommitAsync();
            Assert(!await setup.Warehouses.AnyAsync(item => item.WarehouseCode == token), "only isolated workflow fixture removed");
        }
    }

    private static async Task<string> CompleteExportAsync(DbContextOptions<InventoryDbContext> options, UserSession session, int id)
    {
        await using var db = new InventoryDbContext(options);
        try { await new ExportService(db, new AuthorizationService(db)).CompleteAsync(session, id); return "completed"; }
        catch (InvalidOperationException ex) when (ex.GetBaseException() is Microsoft.Data.SqlClient.SqlException { Number: 1205 }) { return "deadlock-conflict (SQL 1205)"; }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ALREADY_COMPLETED") || ex.Message.Contains("hoàn tất") || ex.Message.Contains("hoàn thành")) { return "completed-state-conflict"; }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 1205 }) { return "deadlock-conflict"; }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1205) { return "deadlock-conflict"; }
    }
    private static void Assert(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
}
