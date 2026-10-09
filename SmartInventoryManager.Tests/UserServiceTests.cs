using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Common.Security;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Tests;

[TestClass]
public class UserServiceTests
{
    [TestMethod]
    public async Task DeactivateAsync_AdminDeactivatesOtherUserWithoutDeletingHistory()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { RoleId = 1, RoleName = RoleNames.Admin });
        context.Roles.Add(new Role { RoleId = 2, RoleName = RoleNames.Staff });
        var admin = new User { UserId = 1, Username = "admin", FullName = "Admin", PasswordHash = "hash", RoleId = 1, IsActive = true, CreatedAt = DateTime.Now };
        var staff = new User { UserId = 2, Username = "staff", FullName = "Staff", PasswordHash = "hash", RoleId = 2, IsActive = true, CreatedAt = DateTime.Now };
        context.Users.AddRange(admin, staff);
        await context.SaveChangesAsync();

        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        var service = new UserService(context, new AuthorizationService(context));

        await service.DeactivateAsync(session, staff.UserId);

        var stored = await context.Users.SingleAsync(user => user.UserId == staff.UserId);
        Assert.IsFalse(stored.IsActive);
        Assert.IsTrue(await context.Users.AnyAsync(user => user.UserId == staff.UserId));
    }

    [TestMethod]
    public async Task DeactivateAsync_RejectsSelfDeactivation()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { RoleId = 1, RoleName = RoleNames.Admin });
        context.Users.Add(new User { UserId = 1, Username = "admin", FullName = "Admin", PasswordHash = "hash", RoleId = 1, IsActive = true, CreatedAt = DateTime.Now });
        await context.SaveChangesAsync();

        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        var service = new UserService(context, new AuthorizationService(context));

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.DeactivateAsync(session, 1));
    }

    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InventoryDbContext(options);
    }
}
