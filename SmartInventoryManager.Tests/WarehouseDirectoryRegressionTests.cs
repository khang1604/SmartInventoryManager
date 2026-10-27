using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class WarehouseDirectoryRegressionTests
{
    // Tạo DbContext và dữ liệu minh họa chung cho nhóm ca kiểm thử.
    private static InventoryDbContext Fixture()
    {
        var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var admin = new Role { RoleId = 1, RoleName = RoleNames.Admin };
        var manager = new Role { RoleId = 2, RoleName = RoleNames.WarehouseManager };
        var staff = new Role { RoleId = 3, RoleName = RoleNames.Staff };
        db.Users.AddRange(
            new User { UserId = 1, Username = "SYN_ADMIN", FullName = "Synthetic Admin", PasswordHash = "synthetic-not-a-credential", IsActive = true, Role = admin },
            new User { UserId = 2, Username = "SYN_MANAGER", FullName = "Synthetic Manager", PasswordHash = "synthetic-not-a-credential", IsActive = true, Role = manager },
            new User { UserId = 3, Username = "SYN_STAFF", FullName = "Synthetic Staff", PasswordHash = "synthetic-not-a-credential", IsActive = true, Role = staff },
            new User { UserId = 4, Username = "SYN_OTHER", FullName = "Synthetic Other Manager", PasswordHash = "synthetic-not-a-credential", IsActive = true, Role = manager });
        db.Warehouses.AddRange(
            new Warehouse { WarehouseId = 5, WarehouseCode = "QA05", WarehouseName = "Synthetic inactive own", ManagerId = 2, IsActive = false },
            new Warehouse { WarehouseId = 6, WarehouseCode = "QA06", WarehouseName = "Synthetic active own", ManagerId = 2, IsActive = true },
            new Warehouse { WarehouseId = 7, WarehouseCode = "QA07", WarehouseName = "Synthetic inactive other", ManagerId = 4, IsActive = false },
            new Warehouse { WarehouseId = 8, WarehouseCode = "QA08", WarehouseName = "Synthetic active other", ManagerId = 4, IsActive = true });
        foreach (var id in new[] { 5, 6, 8 }) db.UserWarehouses.Add(new UserWarehouse { UserId = 3, WarehouseId = id, IsActive = true });
        db.SaveChanges(); return db;
    }
    // Tạo phiên đăng nhập giả lập theo người dùng và vai trò của ca kiểm thử.
    private static UserSession Session(int id, string role)
    {
        var s = new UserSession(); s.SignIn(new(id, "SYNTHETIC", "Synthetic", null, role)); return s;
    }

    // Kiểm tra ADMIN tra cứu, kích hoạt lại kho ngừng hoạt động mà không cấp nhầm quyền tồn kho.
    [TestMethod]
    public async Task AdminCanFindInactiveWarehouseAndReactivateWithoutGrantingStockAccess()
    {
        await using var db = Fixture(); var auth = new AuthorizationService(db); var service = new WarehouseService(db, auth); var admin = Session(1, RoleNames.Admin);
        await service.SetActiveAsync(admin, 6, false);
        Assert.IsTrue((await service.GetAllAsync(admin)).Any(row => row.WarehouseId == 6 && !row.IsActive));
        Assert.IsFalse((await service.GetActiveAsync(admin)).Any(row => row.Id == 6));
        Assert.IsFalse(await auth.CanAdjustInventoryAsync(admin, 6));
        Assert.IsFalse(await auth.CanCreateDraftReceiptAsync(admin, 6));
        Assert.AreEqual(4, await db.Warehouses.CountAsync(), "Status change must never delete a warehouse.");
        await service.SetActiveAsync(admin, 6, true);
        Assert.IsTrue((await service.GetActiveAsync(admin)).Any(row => row.Id == 6));
    }

    // Kiểm tra danh mục kho chỉ gồm kho quản lý, còn lựa chọn vận hành chỉ dùng kho hoạt động.
    [TestMethod]
    public async Task ManagerDirectoryIncludesOnlyOwnWarehousesWhileOperationalLookupStaysActive()
    {
        await using var db = Fixture(); var auth = new AuthorizationService(db); var manager = Session(2, RoleNames.WarehouseManager); var service = new WarehouseService(db, auth);
        CollectionAssert.AreEquivalent(new[] { 5, 6 }, (await service.GetAllAsync(manager)).Select(x => x.WarehouseId).ToArray());
        CollectionAssert.AreEqual(new[] { 6 }, (await service.GetActiveAsync(manager)).Select(x => x.Id).ToArray());
        Assert.IsFalse(await auth.CanViewWarehouseAsync(manager, 5));
        Assert.IsFalse(await auth.CanViewWarehouseAsync(manager, 8));
    }

    // Kiểm tra danh sách kho của nhân viên vẫn cần phân công, kho hiện tại và tài khoản còn hiệu lực.
    [TestMethod]
    public async Task StaffDirectoryStillRequiresActiveAssignedCurrentWarehouseAndActiveActor()
    {
        await using var db = Fixture(); var auth = new AuthorizationService(db); var service = new WarehouseService(db, auth); var staff = Session(3, RoleNames.Staff);
        Assert.AreEqual(0, (await service.GetAllAsync(staff)).Count);
        staff.SetCurrentWarehouse(5); Assert.AreEqual(0, (await service.GetAllAsync(staff)).Count);
        staff.SetCurrentWarehouse(6); CollectionAssert.AreEqual(new[] { 6 }, (await service.GetAllAsync(staff)).Select(x => x.WarehouseId).ToArray());
        (await db.Users.SingleAsync(x => x.UserId == 3)).IsActive = false; await db.SaveChangesAsync();
        Assert.AreEqual(0, (await service.GetAllAsync(staff)).Count);
        var admin = Session(1, RoleNames.Admin); (await db.Users.SingleAsync(x => x.UserId == 1)).IsActive = false; await db.SaveChangesAsync();
        Assert.AreEqual(0, (await service.GetAllAsync(admin)).Count);
    }

    // Kiểm tra cả hai cách cập nhật đều chặn ngừng danh mục còn sản phẩm hoạt động.
    [TestMethod]
    public async Task CategoryWithActiveProductCannotBeDeactivatedThroughEitherWritePath()
    {
        await using var db = Fixture(); var admin = Session(1, RoleNames.Admin); var service = new CategoryService(db, new AuthorizationService(db));
        db.Categories.Add(new Category { CategoryId = 11, CategoryName = "Synthetic Category", IsActive = true });
        db.Products.Add(new Product { ProductId = 11, ProductCode = "SYN-P11", ProductName = "Synthetic Product", Unit = "unit", CategoryId = 11, IsActive = true, ReorderPoint = 1, MaximumStock = 10 });
        await db.SaveChangesAsync();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetActiveAsync(admin, 11, false));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.UpdateAsync(admin, 11, new CategoryRequestDto { CategoryName = "Synthetic Category", IsActive = false }));
        Assert.IsTrue((await db.Categories.SingleAsync(x => x.CategoryId == 11)).IsActive);
    }
}
