using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public class ReceiptWorkflowTests
{
    [TestMethod]
    public void ReceiptServices_ExposeTheAgreedContracts()
    {
        using var context = CreateContext();
        var authorization = new AuthorizationService(context);

        IImportService importService = new ImportService(context, authorization);
        IExportService exportService = new ExportService(context, authorization);

        Assert.IsNotNull(importService);
        Assert.IsNotNull(exportService);
    }

    [TestMethod]
    public async Task CreateDraftAsync_ImportGeneratesCodeAndAllowsEmptyDraft()
    {
        await using var context = CreateContext();
        context.Warehouses.Add(new Warehouse { WarehouseId = 7, WarehouseCode = "KHO-07", WarehouseName = "Kho 07", IsActive = true });
        context.Suppliers.Add(new Supplier { SupplierId = 3, SupplierCode = "NCC-03", SupplierName = "Nhà cung cấp 03", IsActive = true });
        await context.SaveChangesAsync();

        var session = CreateAdminSession();
        var service = new ImportService(context, new AuthorizationService(context));

        var id = await service.CreateDraftAsync(session, new ImportReceiptDto
        {
            WarehouseId = 7,
            SupplierId = 3
        });

        var receipt = await context.ImportReceipts.Include(item => item.Details).SingleAsync(item => item.ImportReceiptId == id);
        Assert.IsTrue(receipt.ReceiptCode.StartsWith("PN", StringComparison.Ordinal));
        Assert.AreEqual("DRAFT", receipt.Status);
        Assert.AreEqual(0, receipt.Details.Count);
        Assert.AreEqual(0m, receipt.TotalAmount);
    }

    [TestMethod]
    public async Task UpdateDraftAsync_RejectedImportReturnsToDraft()
    {
        await using var context = CreateContext();
        context.Warehouses.Add(new Warehouse { WarehouseId = 7, WarehouseCode = "KHO-07", WarehouseName = "Kho 07", IsActive = true });
        context.Suppliers.Add(new Supplier { SupplierId = 3, SupplierCode = "NCC-03", SupplierName = "Nhà cung cấp 03", IsActive = true });
        context.ImportReceipts.Add(new ImportReceipt
        {
            ImportReceiptId = 10,
            ReceiptCode = "PN-OLD",
            WarehouseId = 7,
            SupplierId = 3,
            CreatedBy = 1,
            ReceiptDate = DateTime.Now,
            Status = "REJECTED"
        });
        await context.SaveChangesAsync();

        var service = new ImportService(context, new AuthorizationService(context));
        await service.UpdateDraftAsync(CreateAdminSession(), 10, new ImportReceiptDto
        {
            WarehouseId = 7,
            SupplierId = 3
        });

        var receipt = await context.ImportReceipts.SingleAsync(item => item.ImportReceiptId == 10);
        Assert.AreEqual("DRAFT", receipt.Status);
        Assert.AreEqual("PN-OLD", receipt.ReceiptCode);
    }

    [TestMethod]
    public async Task CreateDraftAsync_ExportGeneratesCodeAndAllowsEmptyDraft()
    {
        await using var context = CreateContext();
        context.Warehouses.Add(new Warehouse { WarehouseId = 7, WarehouseCode = "KHO-07", WarehouseName = "Kho 07", IsActive = true });
        await context.SaveChangesAsync();

        var service = new ExportService(context, new AuthorizationService(context));
        var id = await service.CreateDraftAsync(CreateAdminSession(), new ExportReceiptDto { WarehouseId = 7 });

        var receipt = await context.ExportReceipts.SingleAsync(item => item.ExportReceiptId == id);
        Assert.IsTrue(receipt.ReceiptCode.StartsWith("PX", StringComparison.Ordinal));
        Assert.AreEqual("DRAFT", receipt.Status);
    }

    [TestMethod]
    public async Task CreateDraftAsync_CalculatesTotalFromDetails()
    {
        await using var context = CreateContext();
        SeedReceiptData(context);
        var service = new ImportService(context, new AuthorizationService(context));

        var id = await service.CreateDraftAsync(CreateAdminSession(), new ImportReceiptDto
        {
            WarehouseId = 7,
            SupplierId = 3,
            Details = [new ReceiptDetailDto(11, 2, 125.50m)]
        });

        var receipt = await context.ImportReceipts.SingleAsync(item => item.ImportReceiptId == id);
        Assert.AreEqual(251m, receipt.TotalAmount);
    }

    [TestMethod]
    public async Task AdminCanUseImportWorkflowActions()
    {
        await using var context = CreateContext();
        SeedReceiptData(context);
        var service = new ImportService(context, new AuthorizationService(context));
        var session = CreateAdminSession();

        var id = await service.CreateDraftAsync(session, new ImportReceiptDto
        {
            WarehouseId = 7,
            SupplierId = 3,
            Details = [new ReceiptDetailDto(11, 5, 100)]
        });

        await service.SubmitAsync(session, id);
        await service.RejectAsync(session, id, "Kiểm tra lại số lượng");
        await service.ResubmitAsync(session, id);
        await service.CancelDraftAsync(session, id);

        var receipt = await context.ImportReceipts.SingleAsync(item => item.ImportReceiptId == id);
        Assert.AreEqual("CANCELLED", receipt.Status);

        var approvedId = await service.CreateDraftAsync(session, new ImportReceiptDto
        {
            WarehouseId = 7,
            SupplierId = 3,
            Details = [new ReceiptDetailDto(11, 2, 100)]
        });
        await service.SubmitAsync(session, approvedId);
        await service.ApproveAsync(session, approvedId);

        var approvedReceipt = await context.ImportReceipts.SingleAsync(item => item.ImportReceiptId == approvedId);
        Assert.AreEqual("APPROVED", approvedReceipt.Status);
    }

    [TestMethod]
    public async Task AdminCanDeleteExportDraft()
    {
        await using var context = CreateContext();
        context.Warehouses.Add(new Warehouse { WarehouseId = 7, WarehouseCode = "KHO-07", WarehouseName = "Kho 07", IsActive = true });
        await context.SaveChangesAsync();
        var service = new ExportService(context, new AuthorizationService(context));

        var id = await service.CreateDraftAsync(CreateAdminSession(), new ExportReceiptDto { WarehouseId = 7 });
        await service.DeleteDraftAsync(CreateAdminSession(), id);

        Assert.IsFalse(await context.ExportReceipts.AnyAsync(item => item.ExportReceiptId == id));
    }

    [TestMethod]
    public async Task RejectedReceiptCannotBeCancelled()
    {
        await using var context = CreateContext();
        SeedReceiptData(context);
        var service = new ImportService(context, new AuthorizationService(context));
        var id = await service.CreateDraftAsync(CreateAdminSession(), new ImportReceiptDto
        {
            WarehouseId = 7,
            SupplierId = 3,
            Details = [new ReceiptDetailDto(11, 1, 100)]
        });

        await service.SubmitAsync(CreateAdminSession(), id);
        await service.RejectAsync(CreateAdminSession(), id, "Cần bổ sung chứng từ");

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            service.CancelDraftAsync(CreateAdminSession(), id));
        Assert.AreEqual("REJECTED", (await context.ImportReceipts.SingleAsync(item => item.ImportReceiptId == id)).Status);
    }

    [TestMethod]
    public async Task AdminCanAccessAllActiveWarehouses()
    {
        await using var context = CreateContext();
        context.Warehouses.AddRange(
            new Warehouse { WarehouseId = 7, WarehouseCode = "KHO-07", WarehouseName = "Kho 07", IsActive = true },
            new Warehouse { WarehouseId = 8, WarehouseCode = "KHO-08", WarehouseName = "Kho 08", IsActive = true },
            new Warehouse { WarehouseId = 9, WarehouseCode = "KHO-09", WarehouseName = "Kho 09", IsActive = false });
        await context.SaveChangesAsync();

        var authorization = new AuthorizationService(context);
        var warehouses = await authorization.GetAccessibleWarehouseIdsAsync(CreateAdminSession());

        CollectionAssert.AreEquivalent(new[] { 7, 8 }, warehouses.ToArray());
    }

    private static void SeedReceiptData(InventoryDbContext context)
    {
        context.Warehouses.Add(new Warehouse { WarehouseId = 7, WarehouseCode = "KHO-07", WarehouseName = "Kho 07", IsActive = true });
        context.Suppliers.Add(new Supplier { SupplierId = 3, SupplierCode = "NCC-03", SupplierName = "Nhà cung cấp 03", IsActive = true });
        context.Products.Add(new Product
        {
            ProductId = 11,
            ProductCode = "SP-11",
            ProductName = "Sản phẩm 11",
            Unit = "Cái",
            IsActive = true,
            PurchasePrice = 100,
            SellingPrice = 120
        });
        context.SaveChanges();
    }

    private static UserSession CreateAdminSession()
    {
        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        return session;
    }

    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new InventoryDbContext(options);
        context.Users.Add(new User { UserId = 1, Username = "admin", FullName = "Admin", PasswordHash = "hash", IsActive = true, Role = new Role { RoleId = 1, RoleName = RoleNames.Admin } });
        context.SaveChanges();
        return context;
    }
}
