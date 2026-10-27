using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class MultipleWarehouseAssignmentTests
{
    // Kiểm tra chọn nhiều kho được lưu đồng thời và giữ các phân công đã thu hồi.
    [TestMethod]
    public async Task MultiSelectionSavesTogetherAndRetainsRevokedAssignments()
    {
        await using var db = Fixture();
        var service = new WarehouseAssignmentService(db, new AuthorizationService(db));
        var admin = Session(1, RoleNames.Admin);
        await service.SetWarehousesAsync(admin, 3, new[] { 1, 2, 2 });
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync(x => x.IsActive));
        await service.SetWarehousesAsync(admin, 3, new[] { 2 });
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.IsFalse((await db.UserWarehouses.SingleAsync(x => x.WarehouseId == 1)).IsActive);
        Assert.IsTrue((await db.UserWarehouses.SingleAsync(x => x.WarehouseId == 2)).IsActive);
        await service.SetWarehousesAsync(admin, 3, Array.Empty<int>());
        Assert.AreEqual(0, await db.UserWarehouses.CountAsync(x => x.IsActive));
        await service.SetWarehousesAsync(admin, 3, new[] { 1, 2 });
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync(x => x.IsActive));
    }

    // Kiểm tra danh sách kho không hợp lệ không thu hồi một phần phân công đang có.
    [TestMethod]
    public async Task InvalidMultiSelectionDoesNotPartiallyRevokeExistingAssignments()
    {
        await using var db = Fixture();
        var service = new WarehouseAssignmentService(db, new AuthorizationService(db));
        var admin = Session(1, RoleNames.Admin);
        await service.SetWarehousesAsync(admin, 3, new[] { 1, 2 });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetWarehousesAsync(admin, 3, new[] { 3 }));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetWarehousesAsync(admin, 3, new[] { 999 }));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.SetWarehousesAsync(Session(2, RoleNames.WarehouseManager), 3, Array.Empty<int>()));
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync(x => x.IsActive));
        Assert.AreEqual(2, await db.SystemLogs.CountAsync());
    }

    // Kiểm tra kho ngừng hoạt động chỉ được giữ hoặc thu hồi phân công, không kích hoạt lại phân công.
    [TestMethod]
    public async Task InactiveAssignedWarehouseCanBeRetainedOrRevokedButNotReactivated()
    {
        await using var db = Fixture();
        db.UserWarehouses.Add(new UserWarehouse { UserId = 3, WarehouseId = 3, IsActive = true });
        await db.SaveChangesAsync();
        var service = new WarehouseAssignmentService(db, new AuthorizationService(db));
        var admin = Session(1, RoleNames.Admin);
        await service.SetWarehousesAsync(admin, 3, new[] { 1, 3 });
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync(x => x.IsActive));
        await service.SetWarehousesAsync(admin, 3, new[] { 1 });
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetWarehousesAsync(admin, 3, new[] { 1, 3 }));
        Assert.IsFalse((await db.UserWarehouses.SingleAsync(x => x.WarehouseId == 3)).IsActive);
    }

    // Tạo DbContext và dữ liệu minh họa chung cho nhóm ca kiểm thử.
    private static InventoryDbContext Fixture()
    {
        var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.AddRange(new Role { RoleId = 1, RoleName = RoleNames.Admin },
            new Role { RoleId = 2, RoleName = RoleNames.WarehouseManager },
            new Role { RoleId = 3, RoleName = RoleNames.Staff });
        foreach (var (id, role) in new[] { (1, 1), (2, 2), (3, 3) })
            db.Users.Add(new User { UserId = id, RoleId = role, Username = "Synthetic" + id,
                FullName = "Synthetic User " + id, PasswordHash = "synthetic-unused", IsActive = true });
        foreach (var id in new[] { 1, 2, 3 })
            db.Warehouses.Add(new Warehouse { WarehouseId = id, WarehouseCode = "SYN" + id,
                WarehouseName = "Synthetic Warehouse " + id, ManagerId = 2, IsActive = id != 3 });
        db.SaveChanges();
        return db;
    }

    // Tạo phiên đăng nhập giả lập theo người dùng và vai trò của ca kiểm thử.
    private static UserSession Session(int id, string role)
    {
        var session = new UserSession();
        session.SignIn(new(id, "Synthetic" + id, "Synthetic User", null, role));
        return session;
    }

    // Kiểm tra thêm kho giữ các phạm vi khác và thu hồi chỉ ảnh hưởng kho được chọn.
    [TestMethod]
    public async Task AddingWarehousePreservesOtherScopesAndRevocationOnlyAffectsSelectedWarehouse()
    {
        await using var db = Fixture();
        var auth = new AuthorizationService(db);
        var service = new WarehouseAssignmentService(db, auth);
        var admin = Session(1, RoleNames.Admin);
        var staff = Session(3, RoleNames.Staff);
        await service.SetActiveAsync(admin, 3, 1, true);
        staff.SetCurrentWarehouse(1);
        Assert.IsTrue(await auth.CanViewWarehouseAsync(staff, 1));
        await service.SetActiveAsync(admin, 3, 2, true);
        Assert.IsTrue(await auth.CanViewWarehouseAsync(staff, 1));
        staff.SetCurrentWarehouse(2);
        Assert.IsTrue(await auth.CanViewWarehouseAsync(staff, 2));
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync(x => x.IsActive));
        await service.SetActiveAsync(admin, 3, 1, false);
        staff.SetCurrentWarehouse(1);
        Assert.IsFalse(await auth.CanViewWarehouseAsync(staff, 1));
        staff.SetCurrentWarehouse(2);
        Assert.IsTrue(await auth.CanViewWarehouseAsync(staff, 2));
        await service.SetActiveAsync(admin, 3, 1, true);
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync(x => x.IsActive));
        Assert.AreEqual(1, await db.SystemLogs.CountAsync(x => x.Action == "DEACTIVATE_ASSIGNMENT"));
    }

    // Kiểm tra kích hoạt lại phân công không thu hồi kho khác hoặc tạo dòng trùng.
    [TestMethod]
    public async Task ReactivatingExistingWarehouseDoesNotDeactivateOtherAssignmentsOrDuplicateRows()
    {
        await using var db = Fixture();
        db.UserWarehouses.AddRange(new UserWarehouse { UserId = 3, WarehouseId = 1 },
            new UserWarehouse { UserId = 3, WarehouseId = 2 });
        await db.SaveChangesAsync();
        await new WarehouseAssignmentService(db, new AuthorizationService(db))
            .SetActiveAsync(Session(1, RoleNames.Admin), 3, 2, true);
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync(x => x.IsActive));
    }

    // Kiểm tra tài khoản hoặc người thao tác không hợp lệ không xóa được phân công hiện có.
    [TestMethod]
    public async Task InvalidTargetOrActorCannotRemoveExistingAssignment()
    {
        await using var db = Fixture();
        var service = new WarehouseAssignmentService(db, new AuthorizationService(db));
        var admin = Session(1, RoleNames.Admin);
        await service.SetActiveAsync(admin, 3, 1, true);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetActiveAsync(admin, 3, 3, true));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetActiveAsync(admin, 2, 2, true));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.SetActiveAsync(Session(2, RoleNames.WarehouseManager), 3, 2, true));
        (await db.Users.SingleAsync(x => x.UserId == 3)).IsActive = false;
        await db.SaveChangesAsync();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SetActiveAsync(admin, 3, 2, true));
        Assert.AreEqual(1, (await db.UserWarehouses.SingleAsync()).WarehouseId);
        Assert.IsTrue((await db.UserWarehouses.SingleAsync()).IsActive);
    }

    // Kiểm tra quản lý vẫn được truy cập nhiều kho mình phụ trách.
    [TestMethod]
    public async Task ManagerStillHasAccessToMultipleManagedWarehouses()
    {
        await using var db = Fixture();
        var auth = new AuthorizationService(db);
        var manager = Session(2, RoleNames.WarehouseManager);
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, (await auth.GetAssignedWarehouseIdsAsync(manager)).ToArray());
        Assert.IsTrue(await auth.CanViewWarehouseAsync(manager, 1));
        Assert.IsTrue(await auth.CanViewWarehouseAsync(manager, 2));
    }
}
