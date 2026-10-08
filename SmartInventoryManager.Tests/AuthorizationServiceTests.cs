using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

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
        var service = CreateAuthorizationService();
        var session = CreateSession(RoleNames.Staff);
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