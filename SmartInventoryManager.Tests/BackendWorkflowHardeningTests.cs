using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Events;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public class BackendWorkflowHardeningTests
{
    [TestMethod]
    public async Task AdjustAsync_UpdatesInventoryAndWritesHistory()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        var service = new InventoryService(context, new AuthorizationService(context));

        await service.AdjustAsync(CreateSession(RoleNames.Admin), 7, 11, 5, "Điều chỉnh kiểm kê");

        Assert.AreEqual(5, await context.Inventories.Where(item => item.WarehouseId == 7 && item.ProductId == 11).Select(item => item.Quantity).SingleAsync());
        Assert.AreEqual(1, await context.InventoryTransactions.CountAsync(item => item.ProductId == 11));
    }

    [TestMethod]
    public async Task TransferAsync_UpdatesBothWarehousesAndWritesBothHistoryRows()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11, managerId: 2);
        context.Warehouses.Add(new Warehouse { WarehouseId = 8, WarehouseCode = "KHO-08", WarehouseName = "Kho 08", IsActive = true, ManagerId = 2 });
        context.Inventories.Add(new Inventory { WarehouseId = 7, ProductId = 11, Quantity = 10, UpdatedAt = DateTime.Now });
        await context.SaveChangesAsync();
        var service = new InventoryService(context, new AuthorizationService(context));

        await service.TransferAsync(CreateSession(RoleNames.WarehouseManager, 2), 7, 8, 11, 4, "Điều chuyển nội bộ");

        Assert.AreEqual(6, await context.Inventories.Where(item => item.WarehouseId == 7).Select(item => item.Quantity).SingleAsync());
        Assert.AreEqual(4, await context.Inventories.Where(item => item.WarehouseId == 8).Select(item => item.Quantity).SingleAsync());
        Assert.AreEqual(2, await context.InventoryTransactions.CountAsync(item => item.ProductId == 11));
    }

    [TestMethod]
    public async Task AdjustAsync_RejectsStaffAndInvalidReasonWithoutChangingStock()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        var service = new InventoryService(context, new AuthorizationService(context));
        var staff = CreateSession(RoleNames.Staff, 3, 7);

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.AdjustAsync(staff, 7, 11, 1, "kiểm kê"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.AdjustAsync(CreateSession(RoleNames.Admin), 7, 11, 1, " "));
        Assert.AreEqual(0, await context.Inventories.CountAsync());
    }

    [TestMethod]
    public async Task StockAlert_RefreshCreatesAndResolvesAlert()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11, reorderPoint: 5, maximumStock: 20);
        context.Inventories.Add(new Inventory { WarehouseId = 7, ProductId = 11, Quantity = 2, UpdatedAt = DateTime.Now });
        await context.SaveChangesAsync();
        var service = new StockAlertService(context, new AuthorizationService(context));

        await service.RefreshAsync(11, 7);
        var alert = await context.StockAlerts.SingleAsync();
        Assert.AreEqual("LOW_STOCK", alert.AlertType);

        var inventory = await context.Inventories.SingleAsync();
        inventory.Quantity = 10;
        await context.SaveChangesAsync();
        await service.RefreshAsync(11, 7);

        Assert.IsTrue(await context.StockAlerts.Where(item => item.AlertId == alert.AlertId).Select(item => item.IsResolved).SingleAsync());
    }

    [TestMethod]
    public async Task StockAlert_RefreshTwiceKeepsOneOpenAlertAndChangesType()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11, reorderPoint: 5, maximumStock: 20);
        context.Inventories.Add(new Inventory { WarehouseId = 7, ProductId = 11, Quantity = 0, UpdatedAt = DateTime.Now });
        await context.SaveChangesAsync();
        var service = new StockAlertService(context, new AuthorizationService(context));

        await service.RefreshAsync(11, 7);
        await service.RefreshAsync(11, 7);
        Assert.AreEqual(1, await context.StockAlerts.CountAsync(item => !item.IsResolved));
        Assert.AreEqual("OUT_OF_STOCK", (await context.StockAlerts.SingleAsync(item => !item.IsResolved)).AlertType);

        context.Inventories.Single().Quantity = 2;
        await context.SaveChangesAsync();
        await service.RefreshAsync(11, 7);

        Assert.AreEqual(1, await context.StockAlerts.CountAsync(item => !item.IsResolved));
        Assert.AreEqual("LOW_STOCK", (await context.StockAlerts.SingleAsync(item => !item.IsResolved)).AlertType);
        Assert.AreEqual(2, await context.StockAlerts.CountAsync());
    }

    [TestMethod]
    public async Task ImportComplete_EmitsEventAfterCommitAndRejectsDuplicatePost()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        context.Suppliers.Add(new Supplier { SupplierId = 3, SupplierCode = "NCC-3", SupplierName = "Nhà cung cấp", IsActive = true });
        await context.SaveChangesAsync();
        var service = new ImportService(context, new AuthorizationService(context));
        ReceiptCompletedEvent? completed = null;
        service.ReceiptCompleted += (_, args) => completed = args;
        var id = await service.CreateDraftAsync(CreateSession(RoleNames.Admin), new ImportReceiptDto
        {
            WarehouseId = 7, SupplierId = 3, Details = [new ReceiptDetailDto(11, 2, 10)]
        });
        await service.SubmitAsync(CreateSession(RoleNames.Admin), id);
        await service.ApproveAsync(CreateSession(RoleNames.Admin), id);
        await service.CompleteAsync(CreateSession(RoleNames.Admin), id);

        Assert.IsNotNull(completed);
        Assert.AreEqual(id, completed!.ReceiptId);
        Assert.AreEqual(2, await context.Inventories.Where(item => item.ProductId == 11).Select(item => item.Quantity).SingleAsync());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.CompleteAsync(CreateSession(RoleNames.Admin), id));
    }

    [TestMethod]
    public async Task ExportComplete_InsufficientStockDoesNotEmitEventOrWriteHistory()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        context.ExportReceipts.Add(new ExportReceipt
        {
            ExportReceiptId = 1,
            ReceiptCode = "PX-1",
            WarehouseId = 7,
            CreatedBy = 1,
            ReceiptDate = DateTime.Now,
            Status = "APPROVED",
            Details = [new ExportDetail { ProductId = 11, Quantity = 5, UnitPrice = 10 }]
        });
        context.Inventories.Add(new Inventory { WarehouseId = 7, ProductId = 11, Quantity = 2, UpdatedAt = DateTime.Now });
        await context.SaveChangesAsync();
        var service = new ExportService(context, new AuthorizationService(context));
        var eventCount = 0;
        service.ReceiptCompleted += (_, _) => eventCount++;

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => service.CompleteAsync(CreateSession(RoleNames.Admin), 1));

        Assert.AreEqual(0, eventCount);
        Assert.AreEqual(2, (await context.Inventories.SingleAsync()).Quantity);
        Assert.AreEqual(0, await context.InventoryTransactions.CountAsync());
        Assert.AreEqual("APPROVED", await context.ExportReceipts.Select(item => item.Status).SingleAsync());
    }

    [TestMethod]
    public async Task StockAlert_ResolveAllowsManagerAndRejectsStaff()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11, managerId: 2);
        context.StockAlerts.Add(new StockAlert { AlertId = 1, ProductId = 11, WarehouseId = 7, AlertType = "LOW_STOCK", CurrentQuantity = 1, Threshold = 5, CreatedAt = DateTime.Now });
        await context.SaveChangesAsync();
        var service = new StockAlertService(context, new AuthorizationService(context));

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.ResolveAsync(CreateSession(RoleNames.Staff, 3, 7), 1));
        await service.ResolveAsync(CreateSession(RoleNames.WarehouseManager, 2), 1);

        Assert.IsTrue(await context.StockAlerts.Where(item => item.AlertId == 1).Select(item => item.IsResolved).SingleAsync());
    }

    [TestMethod]
    public async Task ProductUpdate_RefreshesAlertsWhenThresholdChanges()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11, reorderPoint: 5, maximumStock: 20);
        context.Inventories.Add(new Inventory { WarehouseId = 7, ProductId = 11, Quantity = 8, UpdatedAt = DateTime.Now });
        context.Categories.Add(new Category { CategoryId = 1, CategoryName = "Nhóm 1", IsActive = true });
        await context.SaveChangesAsync();
        var service = new ProductManagementService(context, new AuthorizationService(context));
        var request = new ProductRequestDto
        {
            ProductCode = "SP-11",
            ProductName = "Sản phẩm 11",
            CategoryId = 1,
            Unit = "Cái",
            PurchasePrice = 10,
            SellingPrice = 15,
            ReorderPoint = 10,
            MaximumStock = 20
        };

        await service.UpdateAsync(CreateSession(RoleNames.Admin), 11, request);

        var alert = await context.StockAlerts.SingleAsync();
        Assert.AreEqual("LOW_STOCK", alert.AlertType);
        Assert.AreEqual(10, alert.Threshold);
    }

    [TestMethod]
    public async Task ProductDeactivate_ResolvesOpenAlerts()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11, reorderPoint: 5, maximumStock: 20);
        context.Inventories.Add(new Inventory { WarehouseId = 7, ProductId = 11, Quantity = 1, UpdatedAt = DateTime.Now });
        context.Categories.Add(new Category { CategoryId = 1, CategoryName = "Nhóm 1", IsActive = true });
        await context.SaveChangesAsync();
        await new StockAlertService(context, new AuthorizationService(context)).RefreshAsync(11, 7);
        var service = new ProductManagementService(context, new AuthorizationService(context));

        await service.SetActiveAsync(CreateSession(RoleNames.Admin), 11, false);

        Assert.IsTrue(await context.StockAlerts.Select(item => item.IsResolved).SingleAsync());
    }

    [TestMethod]
    public async Task ExportComplete_WhenLaterDetailFails_RestoresTrackedState()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        context.Products.Add(new Product { ProductId = 12, ProductCode = "SP-12", ProductName = "Sản phẩm 12", Unit = "Cái", IsActive = true, ReorderPoint = 1, MaximumStock = 20 });
        var firstInventory = new Inventory { WarehouseId = 7, ProductId = 11, Quantity = 5, UpdatedAt = DateTime.Now };
        context.Inventories.Add(firstInventory);
        context.Inventories.Add(new Inventory { WarehouseId = 7, ProductId = 12, Quantity = 1, UpdatedAt = DateTime.Now });
        context.ExportReceipts.Add(new ExportReceipt
        {
            ExportReceiptId = 1, ReceiptCode = "PX-1", WarehouseId = 7, CreatedBy = 1, ReceiptDate = DateTime.Now, Status = "APPROVED",
            Details = [new ExportDetail { ProductId = 11, Quantity = 2, UnitPrice = 10 }, new ExportDetail { ProductId = 12, Quantity = 2, UnitPrice = 10 }]
        });
        await context.SaveChangesAsync();
        var service = new ExportService(context, new AuthorizationService(context));

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.CompleteAsync(CreateSession(RoleNames.Admin), 1));

        Assert.AreEqual(5, await context.Inventories.Where(item => item.ProductId == 11).Select(item => item.Quantity).SingleAsync());
        Assert.AreEqual(5, firstInventory.Quantity);
        Assert.AreEqual("APPROVED", await context.ExportReceipts.Select(item => item.Status).SingleAsync());
        Assert.AreEqual(0, await context.InventoryTransactions.CountAsync());
    }

    [TestMethod]
    public async Task CompletedReceipt_CannotBeSubmittedOrEdited()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        context.ImportReceipts.Add(new ImportReceipt { ImportReceiptId = 1, ReceiptCode = "PN-1", WarehouseId = 7, SupplierId = 3, CreatedBy = 1, ReceiptDate = DateTime.Now, Status = "COMPLETED" });
        context.Suppliers.Add(new Supplier { SupplierId = 3, SupplierCode = "NCC-3", SupplierName = "Nhà cung cấp", IsActive = true });
        await context.SaveChangesAsync();
        var service = new ImportService(context, new AuthorizationService(context));
        var request = new ImportReceiptDto { WarehouseId = 7, SupplierId = 3 };

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SubmitAsync(CreateSession(RoleNames.Admin), 1));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateDraftAsync(CreateSession(RoleNames.Admin), 1, request));
    }

    [TestMethod]
    public async Task StaffCanCreateAndSubmitOwnDraft_ButCannotApproveOrComplete()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        context.Suppliers.Add(new Supplier { SupplierId = 3, SupplierCode = "NCC-3", SupplierName = "Nhà cung cấp", IsActive = true });
        await context.SaveChangesAsync();
        var service = new ImportService(context, new AuthorizationService(context));
        var staff = CreateSession(RoleNames.Staff, 3, 7);
        var id = await service.CreateDraftAsync(staff, new ImportReceiptDto { WarehouseId = 7, SupplierId = 3, Details = [new ReceiptDetailDto(11, 1, 10)] });

        await service.SubmitAsync(staff, id);
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.ApproveAsync(staff, id));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.CompleteAsync(staff, id));
    }

    [TestMethod]
    public async Task NotificationService_RejectsReversedDateRange()
    {
        await using var context = CreateContext();
        SeedWarehouseAndProduct(context, 7, 11);
        var service = new NotificationService(context);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => service.GetAsync(
            CreateSession(RoleNames.Admin), from: new DateTime(2026, 2, 1), to: new DateTime(2026, 1, 31)));
    }

    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(builder => builder.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new InventoryDbContext(options);
    }

    private static void SeedWarehouseAndProduct(InventoryDbContext context, int warehouseId, int productId, int? managerId = null, int reorderPoint = 1, int maximumStock = 20)
    {
        context.Users.AddRange(
            new User { UserId = 1, Username = "admin", FullName = "Admin", PasswordHash = "hash", IsActive = true, Role = new Role { RoleId = 1, RoleName = RoleNames.Admin } },
            new User { UserId = 2, Username = "manager", FullName = "Manager", PasswordHash = "hash", IsActive = true, Role = new Role { RoleId = 2, RoleName = RoleNames.WarehouseManager } },
            new User { UserId = 3, Username = "staff", FullName = "Staff", PasswordHash = "hash", IsActive = true, Role = new Role { RoleId = 3, RoleName = RoleNames.Staff } });
        context.UserWarehouses.Add(new UserWarehouse { UserId = 3, WarehouseId = warehouseId });
        context.Warehouses.Add(new Warehouse { WarehouseId = warehouseId, WarehouseCode = $"KHO-{warehouseId}", WarehouseName = $"Kho {warehouseId}", IsActive = true, ManagerId = managerId });
        context.Products.Add(new Product { ProductId = productId, ProductCode = $"SP-{productId}", ProductName = $"Sản phẩm {productId}", Unit = "Cái", IsActive = true, ReorderPoint = reorderPoint, MaximumStock = maximumStock });
        context.SaveChanges();
    }

    private static UserSession CreateSession(string role, int userId = 1, int? warehouseId = null)
    {
        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(userId, role, role, null, role));
        if (warehouseId.HasValue) session.SetCurrentWarehouse(warehouseId.Value);
        return session;
    }
}
