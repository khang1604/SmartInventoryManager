using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public class AuthorizationServiceTests
{
    [TestMethod]
    public void RoleChecks_RequireAuthenticationAndMatchRole()
    {
        var service = CreateAuthorizationService();
        var anonymous = new UserSession();
        var managerSession = CreateSession(RoleNames.WarehouseManager);
        var staffSession = CreateSession(RoleNames.Staff);

        Assert.IsFalse(service.CanView(anonymous));
        Assert.IsFalse(service.IsWarehouseManager(anonymous));
        Assert.IsTrue(service.CanView(managerSession));
        Assert.IsTrue(service.IsWarehouseManager(managerSession));
        Assert.IsFalse(service.IsStaff(managerSession));
        Assert.IsTrue(service.IsStaff(staffSession));
    }

    [TestMethod]
    public void RoleChecks_TrimRoleNameForAllSupportedRoles()
    {
        var service = CreateAuthorizationService();

        var adminSession = CreateSession($"  {RoleNames.Admin}  ");
        var managerSession = CreateSession($" {RoleNames.WarehouseManager} ");
        var staffSession = CreateSession($" {RoleNames.Staff} ");

        Assert.IsTrue(service.IsAdmin(adminSession));
        Assert.IsTrue(service.IsWarehouseManager(managerSession));
        Assert.IsTrue(service.IsStaff(staffSession));
    }

    [TestMethod]
    public void ManagementPermissions_FollowRoleMatrix()
    {
        var service = CreateAuthorizationService();

        Assert.IsTrue(service.CanManageProduct(CreateSession(RoleNames.Admin)));
        Assert.IsTrue(service.CanManageProduct(CreateSession(RoleNames.WarehouseManager)));
        Assert.IsFalse(service.CanManageProduct(CreateSession(RoleNames.Staff)));
        Assert.IsTrue(service.CanManageUser(CreateSession(RoleNames.Admin)));
        Assert.IsFalse(service.CanManageUser(CreateSession(RoleNames.WarehouseManager)));
    }

    [TestMethod]
    public void AdminPermissions_AllowManagementOfAllCrudAreas()
    {
        var service = CreateAuthorizationService();
        var adminSession = CreateSession(RoleNames.Admin);

        Assert.IsTrue(service.CanManageProduct(adminSession));
        Assert.IsTrue(service.CanManageCategory(adminSession));
        Assert.IsTrue(service.CanManageSupplier(adminSession));
        Assert.IsTrue(service.CanManageWarehouse(adminSession));
        Assert.IsTrue(service.CanManageUser(adminSession));
        Assert.IsTrue(service.CanManageRole(adminSession));
    }

    [TestMethod]
    public async Task StaffDraftAccess_RequiresAssignedWarehouse()
    {
        await using var context = CreateAssignedContext();
        var service = new AuthorizationService(context);
        var session = CreateSession(RoleNames.Staff);
        session.SetCurrentWarehouse(7);
        var assignedWarehouses = new HashSet<int> { 7 };

        var assigned = await service.CanCreateDraftReceiptAsync(session, 7, assignedWarehouses);
        var unassigned = await service.CanCreateDraftReceiptAsync(session, 8, assignedWarehouses);
        var canEditAssigned = await service.CanEditDraftReceiptAsync(session, 7, assignedWarehouses);
        var canDeleteUnassigned = await service.CanDeleteDraftReceiptAsync(session, 8, assignedWarehouses);

        Assert.IsTrue(assigned);
        Assert.IsFalse(unassigned);
        Assert.IsTrue(canEditAssigned);
        Assert.IsFalse(canDeleteUnassigned);
    }

    [TestMethod]
    public void UserSession_CurrentWarehouseContext_RequiresAuthenticationAndCanBeChanged()
    {
        var session = new UserSession();

        Assert.ThrowsException<InvalidOperationException>(() => session.SetCurrentWarehouse(7));
        session.SignIn(new AuthenticatedUserDto(1, "user", "User", "user@example.com", RoleNames.Staff));
        session.SetCurrentWarehouse(7);
        Assert.AreEqual(7, session.CurrentWarehouseId);

        session.SetCurrentWarehouse(8);
        Assert.AreEqual(8, session.CurrentWarehouseId);
    }

    [TestMethod]
    public void UserSession_AssignmentInitialization_SelectsOnlySingleStaffWarehouse()
    {
        var session = CreateSession(RoleNames.Staff);

        session.SetCurrentWarehouseFromAssignments(new[] { 7 });
        Assert.AreEqual(7, session.CurrentWarehouseId);

        session.SetCurrentWarehouseFromAssignments(new[] { 7, 8 });
        Assert.IsNull(session.CurrentWarehouseId);
    }

    [TestMethod]
    public async Task StaffScope_RequiresCurrentContextAndPersistedAssignment()
    {
        await using var context = CreateAssignedContext();
        var service = new AuthorizationService(context);
        var session = CreateSession(RoleNames.Staff);
        CollectionAssert.AreEquivalent(new[] { 7 }, (await service.GetAssignedWarehouseIdsAsync(session)).ToArray());
        Assert.AreEqual(0, (await service.GetAccessibleWarehouseIdsAsync(session)).Count);
        session.SetCurrentWarehouse(8);
        Assert.IsFalse(await service.CanViewWarehouseAsync(session, 8));
        Assert.IsFalse(await service.CanCreateDraftReceiptAsync(session, 8, new HashSet<int> { 8 }));
        session.SetCurrentWarehouse(7);
        Assert.IsTrue(await service.CanCreateDraftReceiptAsync(session, 7));
        Assert.IsFalse(await service.CanCreateDraftReceiptAsync(session, 8));
        Assert.IsFalse(await service.CanApproveReceiptAsync(session, 7));
    }

    [TestMethod]
    public async Task StaffScope_RevocationAndInactiveAccountsOrWarehousesFailSafely()
    {
        await using var context = CreateAssignedContext();
        var service = new AuthorizationService(context);
        var session = CreateSession(RoleNames.Staff);
        session.SetCurrentWarehouse(7);
        var assignment = await context.UserWarehouses.SingleAsync();
        context.UserWarehouses.Remove(assignment); await context.SaveChangesAsync();
        Assert.IsFalse(await service.CanCreateDraftReceiptAsync(session, 7));
        context.UserWarehouses.Add(assignment); await context.SaveChangesAsync();
        var warehouse = await context.Warehouses.SingleAsync(x => x.WarehouseId == 7);
        warehouse.IsActive = false; await context.SaveChangesAsync();
        Assert.IsFalse(await service.CanViewWarehouseAsync(session, 7));
        warehouse.IsActive = true;
        var user = await context.Users.SingleAsync(); user.IsActive = false;
        await context.SaveChangesAsync();
        Assert.AreEqual(0, (await service.GetAssignedWarehouseIdsAsync(session)).Count);
        Assert.IsFalse(await service.CanCreateDraftReceiptAsync(session, 7));
    }

    [TestMethod]
    public async Task Scope_RevalidatesPersistedRoleAndMissingUser()
    {
        await using var context = CreateAssignedContext();
        var service = new AuthorizationService(context);
        var staff = CreateSession(RoleNames.Staff); staff.SetCurrentWarehouse(7);
        Assert.IsFalse(await service.CanApproveReceiptAsync(CreateSession(RoleNames.Admin), 7));
        var user = await context.Users.Include(x => x.Role).SingleAsync();
        user.Role.RoleName = RoleNames.Admin; await context.SaveChangesAsync();
        Assert.IsFalse(await service.CanViewWarehouseAsync(staff, 7));
        context.UserWarehouses.RemoveRange(context.UserWarehouses);
        await context.SaveChangesAsync();
        context.Users.Remove(user); await context.SaveChangesAsync();
        Assert.IsFalse(await service.CanApproveReceiptAsync(CreateSession(RoleNames.Admin), 7));
    }

    [TestMethod]
    public async Task WarehouseSelector_ListsAssignmentsBeforeContextIsSelected()
    {
        await using var context = CreateAssignedContext();
        var session = CreateSession(RoleNames.Staff);
        var service = new SmartInventoryManager.Business.Services.WarehouseService(context, new AuthorizationService(context));
        CollectionAssert.AreEqual(new[] { 7 }, (await service.GetActiveAsync(session)).Select(x => x.Id).ToArray());
        Assert.AreEqual(0, (await service.GetAllAsync(session)).Count);
    }

    [TestMethod]
    public async Task ManagerScope_DeniesOtherWarehouseAndDisabledManager()
    {
        await using var context = CreateAssignedContext();
        var user = await context.Users.Include(x => x.Role).SingleAsync();
        user.Role.RoleName = RoleNames.WarehouseManager;
        (await context.Warehouses.SingleAsync(x => x.WarehouseId == 7)).ManagerId = 1;
        await context.SaveChangesAsync();
        var service = new AuthorizationService(context);
        var session = CreateSession(RoleNames.WarehouseManager);
        Assert.IsTrue(await service.CanApproveReceiptAsync(session, 7));
        Assert.IsFalse(await service.CanApproveReceiptAsync(session, 8));
        user.IsActive = false; await context.SaveChangesAsync();
        Assert.IsFalse(await service.CanApproveReceiptAsync(session, 7));
    }

    [TestMethod]
    public void SqlModel_MatchesCommittedMigrationSnapshot()
    {
        using var context = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=True").Options);
        Assert.IsFalse(context.Database.HasPendingModelChanges());
    }

    private static InventoryDbContext CreateAssignedContext()
    {
        var context = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.Add(new User { UserId = 1, Username = "staff", FullName = "Staff", PasswordHash = "hash", IsActive = true,
            Role = new Role { RoleId = 1, RoleName = RoleNames.Staff } });
        context.Warehouses.AddRange(
            new Warehouse { WarehouseId = 7, WarehouseCode = "A", WarehouseName = "A", IsActive = true },
            new Warehouse { WarehouseId = 8, WarehouseCode = "B", WarehouseName = "B", IsActive = true });
        context.UserWarehouses.Add(new UserWarehouse { UserId = 1, WarehouseId = 7 });
        context.SaveChanges(); return context;
    }

    private static UserSession CreateSession(string roleName)
    {
        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "user", "User", "user@example.com", roleName));
        return session;
    }

    private static AuthorizationService CreateAuthorizationService()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>().Options;
        return new AuthorizationService(new InventoryDbContext(options));
    }
}
