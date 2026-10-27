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
    // Kiểm tra ADMIN ngừng tài khoản khác nhưng giữ dữ liệu lịch sử.
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

    // Kiểm tra không cho quản trị viên tự ngừng hoạt động tài khoản của mình.
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

    // Kiểm tra ADMIN cập nhật đúng hồ sơ, vai trò và trạng thái người dùng.
    [TestMethod]
    public async Task UpdateUserAsync_AdminUpdatesProfileRoleAndStatus()
    {
        await using var context = CreateContext();
        context.Roles.AddRange(
            new Role { RoleId = 1, RoleName = RoleNames.Admin },
            new Role { RoleId = 2, RoleName = RoleNames.Staff });
        context.Users.AddRange(
            new User { UserId = 1, Username = "admin", FullName = "Admin", PasswordHash = "hash", RoleId = 1, IsActive = true, CreatedAt = DateTime.Now },
            new User { UserId = 2, Username = "staff", FullName = "Staff", PasswordHash = "hash", RoleId = 2, IsActive = true, CreatedAt = DateTime.Now });
        await context.SaveChangesAsync();

        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        var service = new UserService(context, new AuthorizationService(context));

        await service.UpdateUserAsync(session, 2, new UpdateUserRequest("Staff Updated", "staff@example.com", RoleNames.Admin, false));

        var stored = await context.Users.SingleAsync(user => user.UserId == 2);
        Assert.AreEqual("Staff Updated", stored.FullName);
        Assert.AreEqual("staff@example.com", stored.Email);
        Assert.AreEqual(1, stored.RoleId);
        Assert.IsFalse(stored.IsActive);
    }

    // Kiểm tra đặt lại mật khẩu thay chuỗi băm cũ bằng mật khẩu mới.
    [TestMethod]
    public async Task ResetPasswordAsync_ReplacesOldPasswordHash()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { RoleId = 1, RoleName = RoleNames.Admin });
        var hasher = new PasswordHasher();
        context.Users.Add(new User
        {
            UserId = 1,
            Username = "admin",
            FullName = "Admin",
            PasswordHash = hasher.HashPassword("OldPassword1"),
            RoleId = 1,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await context.SaveChangesAsync();

        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        var service = new UserService(context, new AuthorizationService(context));

        await service.ResetPasswordAsync(session, 1, new ResetUserPasswordRequest("ResetPassword1", "ResetPassword1"));

        var stored = await context.Users.SingleAsync(user => user.UserId == 1);
        Assert.IsTrue(hasher.VerifyPassword("ResetPassword1", stored.PasswordHash));
        Assert.IsFalse(hasher.VerifyPassword("OldPassword1", stored.PasswordHash));
        var log = await context.SystemLogs.SingleAsync();
        Assert.AreEqual("PASSWORD_RESET", log.Action);
        Assert.AreEqual(1, log.UserId);
        Assert.AreEqual(1, log.EntityId);
        Assert.IsFalse(log.Description!.Contains("ResetPassword1", StringComparison.Ordinal));
        Assert.IsFalse(log.Description.Contains(stored.PasswordHash, StringComparison.Ordinal));
    }

    // Kiểm tra đổi mật khẩu cập nhật chuỗi băm của người dùng hiện tại.
    [TestMethod]
    public async Task ChangePasswordAsync_UpdatesPasswordHashForCurrentUser()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { RoleId = 1, RoleName = RoleNames.Admin });
        var hasher = new PasswordHasher();
        context.Users.Add(new User
        {
            UserId = 1,
            Username = "admin",
            FullName = "Admin",
            PasswordHash = hasher.HashPassword("OldPassword1"),
            RoleId = 1,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await context.SaveChangesAsync();

        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        var service = new UserService(context, new AuthorizationService(context));

        await service.ChangePasswordAsync(session, new ChangePasswordRequest("OldPassword1", "NewPassword1", "NewPassword1"));

        var stored = await context.Users.SingleAsync(user => user.UserId == 1);
        Assert.IsTrue(hasher.VerifyPassword("NewPassword1", stored.PasswordHash));
        Assert.IsFalse(hasher.VerifyPassword("OldPassword1", stored.PasswordHash));
    }

    // Kiểm tra đổi mật khẩu từ chối khi mật khẩu hiện tại không đúng.
    [TestMethod]
    public async Task ChangePasswordAsync_RejectsWrongCurrentPassword()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { RoleId = 1, RoleName = RoleNames.Admin });
        var hasher = new PasswordHasher();
        context.Users.Add(new User
        {
            UserId = 1,
            Username = "admin",
            FullName = "Admin",
            PasswordHash = hasher.HashPassword("OldPassword1"),
            RoleId = 1,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await context.SaveChangesAsync();

        var session = new UserSession();
        session.SignIn(new AuthenticatedUserDto(1, "admin", "Admin", null, RoleNames.Admin));
        var service = new UserService(context, new AuthorizationService(context));

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => service.ChangePasswordAsync(
            session, new ChangePasswordRequest("WrongPassword1", "NewPassword1", "NewPassword1")));
        Assert.AreEqual(0, await context.SystemLogs.CountAsync());
    }

    // Tạo DbContext bộ nhớ riêng cho ca kiểm thử.
    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InventoryDbContext(options);
    }
}
