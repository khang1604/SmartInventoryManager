using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class SingleWarehouseAssignmentTests
{
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

    private static UserSession Session(int id, string role)
    {
        var session = new UserSession();
        session.SignIn(new(id, "Synthetic" + id, "Synthetic User", null, role));
        return session;
    }

    [TestMethod]
    public async Task ReassignmentRevokesOldScopeRetainsHistoryAndReusesPreviousRow()
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
        Assert.IsFalse(await auth.CanViewWarehouseAsync(staff, 1));
        staff.SetCurrentWarehouse(2);
        Assert.IsTrue(await auth.CanViewWarehouseAsync(staff, 2));
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.AreEqual(1, await db.UserWarehouses.CountAsync(x => x.IsActive));
        await service.SetActiveAsync(admin, 3, 1, true);
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.AreEqual(1, (await db.UserWarehouses.SingleAsync(x => x.IsActive)).WarehouseId);
        Assert.AreEqual(2, await db.SystemLogs.CountAsync(x => x.Action == "DEACTIVATE_ASSIGNMENT"));
    }

    [TestMethod]
    public async Task SavingOneWarehouseResolvesLegacyMultipleAssignments()
    {
        await using var db = Fixture();
        db.UserWarehouses.AddRange(new UserWarehouse { UserId = 3, WarehouseId = 1 },
            new UserWarehouse { UserId = 3, WarehouseId = 2 });
        await db.SaveChangesAsync();
        await new WarehouseAssignmentService(db, new AuthorizationService(db))
            .SetActiveAsync(Session(1, RoleNames.Admin), 3, 2, true);
        Assert.AreEqual(2, await db.UserWarehouses.CountAsync());
        Assert.AreEqual(2, (await db.UserWarehouses.SingleAsync(x => x.IsActive)).WarehouseId);
    }

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
