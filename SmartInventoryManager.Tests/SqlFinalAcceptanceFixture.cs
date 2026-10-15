using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

internal static class SqlFinalAcceptanceFixture
{
    public static async Task RunAsync(DbContextOptions<InventoryDbContext> options)
    {
        await using var db = new InventoryDbContext(options);
        var token = "FINAL_" + Guid.NewGuid().ToString("N")[..8];
        Console.WriteLine("Final acceptance fixture: " + token);
        var roles = await db.Roles.ToDictionaryAsync(x => x.RoleName, x => x.RoleId);
        User User(string suffix, string role) => new()
        {
            Username = token + suffix, FullName = "Final acceptance " + suffix,
            Email = token + suffix + "@example.test", PasswordHash = "Not used for authentication",
            RoleId = roles[role], IsActive = true
        };
        var first = User("adminA", RoleNames.Admin); var second = User("adminB", RoleNames.Admin);
        var manager = User("manager", RoleNames.WarehouseManager);
        var category = new Category { CategoryName = token, IsActive = true };
        var supplier = new Supplier { SupplierCode = token, SupplierName = token, IsActive = true };
        var warehouse = new Warehouse { WarehouseCode = token, WarehouseName = token, IsActive = true };
        try
        {
            db.AddRange(first, second, manager, category, supplier, warehouse); await db.SaveChangesAsync();
            UserSession Session(User user)
            {
                var session = new UserSession(); session.SignIn(new(user.UserId, user.Username, user.FullName, user.Email, RoleNames.Admin)); return session;
            }
            var actor = Session(first); var other = Session(second);
            async Task Users(Func<UserService, Task> action)
            {
                await using var ctx = new InventoryDbContext(options); await action(new(ctx, new(ctx)));
            }
            var staffId = 0;
            await Users(async service => staffId = await service.CreateAsync(actor, new(token + "staff", "Final Staff", token + "staff@example.test", "FixturePass9!", "FixturePass9!", RoleNames.Staff)));
            await Users(service => service.ResetPasswordAsync(actor, staffId, new("FixtureReset9!", "FixtureReset9!")));
            await using (var auth = new InventoryDbContext(options))
            {
                Assert.IsNotNull(await new AuthService(auth).AuthenticateAsync(token + "staff", "FixtureReset9!"));
                Assert.IsNull(await new AuthService(auth).AuthenticateAsync(token + "staff", "FixturePass9!"));
                var assignments = new WarehouseAssignmentService(auth, new(auth));
                await assignments.SetActiveAsync(actor, staffId, warehouse.WarehouseId, true);
                await assignments.SetActiveAsync(actor, staffId, warehouse.WarehouseId, false);
                Assert.AreEqual(1, await auth.UserWarehouses.CountAsync(x => x.UserId == staffId));
                Assert.IsFalse((await auth.UserWarehouses.SingleAsync(x => x.UserId == staffId)).IsActive);
            }
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var identifier = token + attempt + "@example.test";
                async Task<Exception?> Create(UserSession session, bool username)
                {
                    await using var ctx = new InventoryDbContext(options);
                    return await Capture(() => new UserService(ctx, new(ctx)).CreateAsync(session,
                        new(username ? identifier : token + "other" + attempt, "Identifier race", username ? token + "mail" + attempt + "@example.test" : identifier.ToUpperInvariant(), "FixturePass9!", "FixturePass9!", RoleNames.Staff)));
                }
                var outcomes = await Task.WhenAll(Create(actor, true), Create(other, false));
                Assert.AreEqual(1, outcomes.Count(x => x is null), "Exactly one account may claim a shared login identifier.");
                Assert.AreEqual(1, await db.Users.CountAsync(x => x.Username.ToUpper() == identifier.ToUpper() || x.Email!.ToUpper() == identifier.ToUpper()));
            }
            warehouse.ManagerId = manager.UserId; warehouse.IsActive = false; await db.SaveChangesAsync();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Users(service => service.DeactivateAsync(actor, manager.UserId)));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Users(service => service.UpdateUserAsync(actor, manager.UserId, new("Final Manager", manager.Email, RoleNames.Staff, true))));
            warehouse.ManagerId = null; warehouse.IsActive = true; await db.SaveChangesAsync();
            async Task Assign()
            {
                await using var ctx = new InventoryDbContext(options);
                await new WarehouseService(ctx, new(ctx)).UpdateAsync(actor, warehouse.WarehouseId,
                    new() { WarehouseCode = token, WarehouseName = token, ManagerId = manager.UserId, IsActive = true });
            }
            var race = await Task.WhenAll(Capture(Assign), Capture(() => Users(service => service.UpdateUserAsync(other, manager.UserId, new("Final Manager", manager.Email, RoleNames.Staff, true)))));
            Assert.IsTrue(race.Any(x => x is null));
            db.ChangeTracker.Clear();
            Assert.IsFalse(await db.Warehouses.AnyAsync(x => x.WarehouseId == warehouse.WarehouseId && x.ManagerId != null &&
                !db.Users.Any(u => u.UserId == x.ManagerId && u.IsActive && u.Role.RoleName == RoleNames.WarehouseManager)), "Concurrent assignment and role change must not leave an invalid manager.");

            var product = new Product { ProductCode = token, ProductName = token, CategoryId = category.CategoryId, Unit = "piece", ReorderPoint = 2, MaximumStock = 20, PurchasePrice = 3, IsActive = true };
            db.Products.Add(product); await db.SaveChangesAsync();
            var request = new ImportReceiptDto { ReceiptCode = token + "I", WarehouseId = warehouse.WarehouseId, SupplierId = supplier.SupplierId, Details = [new(product.ProductId, 5, 3)] };
            var imports = new ImportService(db, new(db)); var receiptId = await imports.CreateDraftAsync(actor, request);
            for (var i = 0; i < 6; i++)
            {
                await imports.SubmitAsync(actor, receiptId); await imports.RejectAsync(actor, receiptId, "Review " + i);
                if (i == 0) await imports.UpdateDraftAsync(actor, receiptId, request); else await imports.ResubmitAsync(actor, receiptId);
            }
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => imports.DeleteDraftAsync(actor, receiptId));
            await imports.SubmitAsync(actor, receiptId); await imports.ApproveAsync(actor, receiptId); await imports.CompleteAsync(actor, receiptId);
            db.ChangeTracker.Clear();
            var audit = new ReceiptAuditService(db, new(db));
            var page1 = await audit.GetStatusChangesAsync(actor, true, receiptId); var page2 = await audit.GetStatusChangesAsync(actor, true, receiptId, 2);
            Assert.AreEqual(21, page1.Total); Assert.AreEqual(20, page1.Rows.Count); Assert.AreEqual(1, page2.Rows.Count);
            Assert.AreEqual(21, page1.Rows.Concat(page2.Rows).Select(x => x.Id).Distinct().Count());
            Assert.AreEqual(6, (await audit.GetRejectionsAsync(actor, true, receiptId)).Total);
            var receipt = await db.ImportReceipts.AsNoTracking().SingleAsync(x => x.ImportReceiptId == receiptId);
            Assert.AreEqual(first.UserId, receipt.SubmittedBy); Assert.AreEqual(first.UserId, receipt.ApprovedBy); Assert.AreEqual(first.UserId, receipt.CompletedBy);
            Assert.IsNotNull(receipt.SubmittedAt); Assert.IsNotNull(receipt.ApprovedAt); Assert.IsNotNull(receipt.CompletedAt); Assert.AreEqual("Review 5", receipt.RejectReason);
            var exports = new ExportService(db, new(db));
            var exportId = await exports.CreateDraftAsync(actor, new() { ReceiptCode = token + "E", WarehouseId = warehouse.WarehouseId, Details = [new(product.ProductId, 1, 4)] });
            await exports.SubmitAsync(actor, exportId); await exports.ApproveAsync(actor, exportId); await exports.CancelAsync(actor, exportId, "Final acceptance cancellation");
            db.ChangeTracker.Clear(); var cancelled = await db.ExportReceipts.AsNoTracking().SingleAsync(x => x.ExportReceiptId == exportId);
            Assert.AreEqual(first.UserId, cancelled.CancelledBy); Assert.IsNotNull(cancelled.CancelledAt); Assert.AreEqual("Final acceptance cancellation", cancelled.CancelReason);
            Assert.AreEqual(3, (await audit.GetStatusChangesAsync(actor, false, exportId)).Total);
            Assert.AreEqual(5, (await db.Inventories.AsNoTracking().SingleAsync(x => x.ProductId == product.ProductId)).Quantity);
            Assert.AreEqual(1, await db.InventoryTransactions.CountAsync(x => x.ProductId == product.ProductId));
            Assert.IsFalse(await new BootstrapAdminService(db).CreateFirstAdminAsync(new(token + "bootstrap", "Unused bootstrap", null, "FixturePass9!", "FixturePass9!", RoleNames.Admin)));
        }
        finally
        {
            db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync();
            var users = await db.Users.Where(x => x.Username.StartsWith(token)).Select(x => x.UserId).ToArrayAsync();
            var imports = await db.ImportReceipts.Where(x => warehouse.WarehouseId > 0 && x.WarehouseId == warehouse.WarehouseId).Select(x => x.ImportReceiptId).ToArrayAsync();
            var exports = await db.ExportReceipts.Where(x => warehouse.WarehouseId > 0 && x.WarehouseId == warehouse.WarehouseId).Select(x => x.ExportReceiptId).ToArrayAsync();
            await db.Notifications.Where(x => users.Contains(x.UserId) || x.Message.Contains(token)).ExecuteDeleteAsync();
            await db.SystemLogs.Where(x => users.Contains(x.UserId ?? 0)).ExecuteDeleteAsync();
            await db.ReceiptRejections.Where(x => imports.Contains(x.ImportReceiptId ?? 0) || exports.Contains(x.ExportReceiptId ?? 0)).ExecuteDeleteAsync();
            await db.ReceiptStatusChanges.Where(x => imports.Contains(x.ImportReceiptId ?? 0) || exports.Contains(x.ExportReceiptId ?? 0)).ExecuteDeleteAsync();
            await db.ImportDetails.Where(x => imports.Contains(x.ImportReceiptId)).ExecuteDeleteAsync();
            await db.ExportDetails.Where(x => exports.Contains(x.ExportReceiptId)).ExecuteDeleteAsync();
            await db.InventoryTransactions.Where(x => x.WarehouseId == warehouse.WarehouseId && warehouse.WarehouseId > 0).ExecuteDeleteAsync();
            await db.StockAlerts.Where(x => x.WarehouseId == warehouse.WarehouseId && warehouse.WarehouseId > 0).ExecuteDeleteAsync();
            await db.ImportReceipts.Where(x => imports.Contains(x.ImportReceiptId)).ExecuteDeleteAsync();
            await db.ExportReceipts.Where(x => exports.Contains(x.ExportReceiptId)).ExecuteDeleteAsync();
            await db.Inventories.Where(x => x.WarehouseId == warehouse.WarehouseId && warehouse.WarehouseId > 0).ExecuteDeleteAsync();
            await db.Products.Where(x => x.ProductCode == token).ExecuteDeleteAsync();
            await db.UserWarehouses.Where(x => users.Contains(x.UserId)).ExecuteDeleteAsync();
            await db.Warehouses.Where(x => x.WarehouseId == warehouse.WarehouseId && x.WarehouseCode == token).ExecuteDeleteAsync();
            await db.Suppliers.Where(x => x.SupplierCode == token).ExecuteDeleteAsync();
            await db.Categories.Where(x => x.CategoryName == token).ExecuteDeleteAsync();
            await db.Users.Where(x => users.Contains(x.UserId)).ExecuteDeleteAsync(); await tx.CommitAsync();
        }
    }

    private static async Task<Exception?> Capture(Func<Task> action)
    {
        try { await action(); return null; }
        catch (InvalidOperationException ex) when (ex.Message.Contains("trùng") || ex.Message.Contains("quản lý")) { return ex; }
        catch (Exception ex) when (ex.GetBaseException() is SqlException { Number: 1205 }) { return ex; }
    }
}
