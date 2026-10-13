using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public class IndependentBackendFixTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Submit_LoadsPersistedDetailsAfterTrackingIsCleared(bool import)
    {
        await using var db = CreateContext();
        Seed(db);
        var authorization = new AuthorizationService(db);
        var session = Admin();
        if (import)
        {
            var service = new ImportService(db, authorization);
            var id = await service.CreateDraftAsync(session, new ImportReceiptDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Details = [new ReceiptDetailDto(1, 2, 10)]
            });
            db.ChangeTracker.Clear();
            await service.SubmitAsync(session, id);
            var receipt = await db.ImportReceipts.SingleAsync(r => r.ImportReceiptId == id);
            Assert.AreEqual("PENDING_APPROVAL", receipt.Status);
            Assert.AreEqual(20m, receipt.TotalAmount);
        }
        else
        {
            var service = new ExportService(db, authorization);
            var id = await service.CreateDraftAsync(session, new ExportReceiptDto
            {
                WarehouseId = 1,
                Details = [new ReceiptDetailDto(1, 2, 10)]
            });
            db.ChangeTracker.Clear();
            await service.SubmitAsync(session, id);
            var receipt = await db.ExportReceipts.SingleAsync(r => r.ExportReceiptId == id);
            Assert.AreEqual("PENDING_APPROVAL", receipt.Status);
            Assert.AreEqual(20m, receipt.TotalAmount);
        }
        Assert.AreEqual(0, await db.Inventories.CountAsync());
        Assert.AreEqual(0, await db.InventoryTransactions.CountAsync());
    }

    [TestMethod]
    public async Task CategoryUpdate_CannotDeactivateCategoryWithActiveProducts()
    {
        await using var db = CreateContext();
        Seed(db);
        var service = new CategoryService(db, new AuthorizationService(db));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateAsync(Admin(), 1,
            new CategoryRequestDto { CategoryName = "Changed", IsActive = false }));
        db.ChangeTracker.Clear();
        var category = await db.Categories.SingleAsync();
        Assert.IsTrue(category.IsActive);
        Assert.AreEqual("Office", category.CategoryName);
    }

    [DataTestMethod]
    [DataRow(1, false)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    public async Task WarehouseManager_MustHaveActiveManagerRole(int managerId, bool accepted)
    {
        await using var db = CreateContext();
        Seed(db);
        var service = new WarehouseService(db, new AuthorizationService(db));
        var request = new WarehouseRequestDto
        {
            WarehouseCode = "NEW",
            WarehouseName = "New Warehouse",
            ManagerId = managerId
        };
        if (accepted)
        {
            var id = await service.CreateAsync(Admin(), request);
            Assert.AreEqual(managerId, (await db.Warehouses.SingleAsync(w => w.WarehouseId == id)).ManagerId);
        }
        else
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.CreateAsync(Admin(), request));
            Assert.AreEqual(1, await db.Warehouses.CountAsync());
        }
    }

    [DataTestMethod]
    [DataRow(10, "NORMAL")]
    [DataRow(0, "OUT_OF_STOCK")]
    [DataRow(25, "OVERSTOCK")]
    public async Task AlertRefresh_UsesUnsavedQuantityAndResolvesStaleType(int quantity, string expected)
    {
        await using var db = CreateContext();
        Seed(db);
        db.Inventories.Add(new Inventory { ProductId = 1, WarehouseId = 1, Quantity = 2 });
        db.StockAlerts.Add(new StockAlert
        {
            ProductId = 1,
            WarehouseId = 1,
            AlertType = "LOW_STOCK",
            CurrentQuantity = 2,
            Threshold = 5
        });
        await db.SaveChangesAsync();
        var inventory = await db.Inventories.SingleAsync();
        inventory.Quantity = quantity;
        await new StockAlertService(db, new AuthorizationService(db))
            .RefreshAsync(1, 1, persistChanges: false, publishEvent: false);
        var alerts = db.ChangeTracker.Entries<StockAlert>().Select(e => e.Entity).ToList();
        Assert.IsTrue(alerts.Single(a => a.AlertType == "LOW_STOCK").IsResolved);
        if (expected == "NORMAL")
            Assert.IsFalse(alerts.Any(a => !a.IsResolved));
        else
        {
            var open = alerts.Single(a => !a.IsResolved);
            Assert.AreEqual(expected, open.AlertType);
            Assert.AreEqual(quantity, open.CurrentQuantity);
        }
        // A scalar query reads the persisted value: refresh did not save.
        Assert.AreEqual(2, await db.Inventories.AsNoTracking().Select(i => i.Quantity).SingleAsync());
    }

    [TestMethod]
    public async Task AlertRefresh_IncludesNewInventoryBeforeFirstSave()
    {
        await using var db = CreateContext();
        Seed(db);
        db.Inventories.Add(new Inventory { ProductId = 1, WarehouseId = 1, Quantity = 7 });
        await new StockAlertService(db, new AuthorizationService(db))
            .RefreshAsync(1, 1, persistChanges: false, publishEvent: false);
        Assert.IsFalse(db.ChangeTracker.Entries<StockAlert>().Any(e => !e.Entity.IsResolved));
        Assert.AreEqual(0, await db.Inventories.AsNoTracking().CountAsync());
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Approve_RejectsPersistedCancellationWhenTrackedStateIsStale(bool import)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new InventoryDbContext(options);
        Seed(db);
        if (import)
            db.ImportReceipts.Add(new ImportReceipt
            {
                ImportReceiptId = 1,
                ReceiptCode = "I1",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedBy = 1,
                Status = "PENDING_APPROVAL"
            });
        else
            db.ExportReceipts.Add(new ExportReceipt
            {
                ExportReceiptId = 1,
                ReceiptCode = "E1",
                WarehouseId = 1,
                CreatedBy = 1,
                Status = "PENDING_APPROVAL"
            });
        await db.SaveChangesAsync();
        await using (var other = new InventoryDbContext(options))
        {
            if (import) (await other.ImportReceipts.SingleAsync()).Status = "CANCELLED";
            else (await other.ExportReceipts.SingleAsync()).Status = "CANCELLED";
            await other.SaveChangesAsync();
        }
        var authorization = new AuthorizationService(db);
        Func<Task> approve = import
            ? () => new ImportService(db, authorization).ApproveAsync(Admin(), 1)
            : () => new ExportService(db, authorization).ApproveAsync(Admin(), 1);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(approve);
        await using var verifier = new InventoryDbContext(options);
        var status = import
            ? await verifier.ImportReceipts.Select(r => r.Status).SingleAsync()
            : await verifier.ExportReceipts.Select(r => r.Status).SingleAsync();
        Assert.AreEqual("CANCELLED", status);
    }

    private static InventoryDbContext CreateContext() => new(new DbContextOptionsBuilder<InventoryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [DataTestMethod]
    [DataRow("category", false)]
    [DataRow("supplier", false)]
    [DataRow("warehouse", false)]
    [DataRow("location", false)]
    [DataRow("product", false)]
    [DataRow("user", false)]
    [DataRow("profile", false)]
    [DataRow("password", false)]
    [DataRow("notification", false)]
    [DataRow("category", true)]
    [DataRow("supplier", true)]
    [DataRow("warehouse", true)]
    [DataRow("location", true)]
    [DataRow("product", true)]
    [DataRow("user", true)]
    [DataRow("profile", true)]
    [DataRow("password", true)]
    [DataRow("notification", true)]
    public async Task Mutations_RejectDisabledOrRoleChangedActor(string operation, bool roleChanged)
    {
        await using var db = CreateContext();
        Seed(db);
        db.Notifications.Add(new Notification { NotificationId = 1, UserId = 1, Title = "Test", Message = "Test" });
        var actor = await db.Users.SingleAsync(u => u.UserId == 1);
        if (roleChanged) actor.RoleId = 2;
        else actor.IsActive = false;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var session = Admin(); // Represents the authenticated DTO retained before revocation.
        var authorization = new AuthorizationService(db);
        Func<Task> action = operation switch
        {
            "category" => () => new CategoryService(db, authorization).CreateAsync(session, new CategoryRequestDto { CategoryName = "New" }),
            "supplier" => () => new SupplierService(db, authorization).CreateAsync(session, new SupplierRequestDto { SupplierCode = "NEW", SupplierName = "New" }),
            "warehouse" => () => new WarehouseService(db, authorization).CreateAsync(session, new WarehouseRequestDto { WarehouseCode = "NEW", WarehouseName = "New" }),
            "location" => () => new LocationService(db, authorization).CreateAsync(session, new LocationRequestDto { WarehouseId = 1, LocationCode = "NEW" }),
            "product" => () => new ProductManagementService(db, authorization).SetActiveAsync(session, 1, false),
            "user" => () => new UserService(db, authorization).DeactivateAsync(session, 2),
            "profile" => () => new UserService(db, authorization).UpdateAccountAsync(session, new UpdateAccountRequest("Changed", null)),
            "password" => () => new UserService(db, authorization).ResetPasswordAsync(session, 2, new ResetUserPasswordRequest("Password123", "Password123")),
            "notification" => () => new NotificationService(db).MarkAsReadAsync(session, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(action);
        Assert.AreEqual(0, db.ChangeTracker.Entries().Count());
        Assert.IsFalse(await db.Notifications.Select(n => n.IsRead).SingleAsync());
        Assert.IsTrue(await db.Products.Select(p => p.IsActive).SingleAsync());
        Assert.IsTrue(await db.Users.Where(u => u.UserId == 2).Select(u => u.IsActive).SingleAsync());
    }

    [TestMethod]
    public async Task InventoryReport_TotalIncludesRowsBeyondDisplayLimit()
    {
        await using var db = CreateContext();
        Seed(db);
        for (var id = 1; id <= 5001; id++)
        {
            if (id > 1) db.Products.Add(new Product
            {
                ProductId = id,
                CategoryId = 1,
                ProductCode = $"P{id}",
                ProductName = "Paper",
                Unit = "piece",
                IsActive = true,
                MaximumStock = 20
            });
            db.Inventories.Add(new Inventory { ProductId = id, WarehouseId = 1, Quantity = 1 });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var report = await new ReportService(db, new AuthorizationService(db)).GetInventoryAsync(Admin(), 1);
        Assert.AreEqual(5001L, report.TotalQuantity);
        Assert.AreEqual(5000, report.Rows.Count);
    }

    [TestMethod]
    public async Task ReceiptReport_TotalsIncludeRowsBeyondLimitAndExcludeDrafts()
    {
        await using var db = CreateContext();
        Seed(db);
        for (var id = 1; id <= 5001; id++)
        {
            db.ImportReceipts.Add(new ImportReceipt
            {
                ImportReceiptId = id,
                ReceiptCode = $"I{id}",
                SupplierId = 1,
                WarehouseId = 1,
                CreatedBy = 1,
                Status = "COMPLETED",
                TotalAmount = 2
            });
            db.ExportReceipts.Add(new ExportReceipt
            {
                ExportReceiptId = id,
                ReceiptCode = $"E{id}",
                WarehouseId = 1,
                CreatedBy = 1,
                Status = "COMPLETED",
                TotalAmount = 3
            });
        }
        db.ExportReceipts.Add(new ExportReceipt
        {
            ExportReceiptId = 5002,
            ReceiptCode = "DRAFT",
            WarehouseId = 1,
            CreatedBy = 1,
            Status = "DRAFT",
            TotalAmount = 999
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var report = await new ReportService(db, new AuthorizationService(db)).GetImportExportAsync(Admin(), 1);
        Assert.AreEqual(10002m, report.ImportTotalAmount);
        Assert.AreEqual(15003m, report.ExportTotalAmount);
        Assert.AreEqual(5000, report.Imports.Count);
        Assert.AreEqual(5000, report.Exports.Count);
    }

    private static UserSession Admin()
    {
        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        return session;
    }

    private static void Seed(InventoryDbContext db)
    {
        db.Users.AddRange(
            new User
            {
                UserId = 1,
                Username = "admin",
                FullName = "Admin",
                PasswordHash = "test",
                IsActive = true,
                Role = new Role { RoleId = 1, RoleName = RoleNames.Admin }
            },
            new User
            {
                UserId = 2,
                Username = "manager",
                FullName = "Manager",
                PasswordHash = "test",
                IsActive = true,
                Role = new Role { RoleId = 2, RoleName = RoleNames.WarehouseManager }
            },
            new User { UserId = 3, Username = "disabled-manager", FullName = "Disabled", PasswordHash = "test", IsActive = false, RoleId = 2 });
        db.Categories.Add(new Category { CategoryId = 1, CategoryName = "Office", IsActive = true });
        db.Products.Add(new Product
        {
            ProductId = 1,
            CategoryId = 1,
            ProductCode = "P1",
            ProductName = "Paper",
            Unit = "piece",
            IsActive = true,
            ReorderPoint = 5,
            MaximumStock = 20
        });
        db.Warehouses.Add(new Warehouse { WarehouseId = 1, WarehouseCode = "WH1", WarehouseName = "Warehouse", IsActive = true });
        db.Suppliers.Add(new Supplier { SupplierId = 1, SupplierCode = "S1", SupplierName = "Supplier", IsActive = true });
        db.SaveChanges();
    }
}
