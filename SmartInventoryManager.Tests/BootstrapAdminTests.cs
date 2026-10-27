using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Common.Security;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Tests;

[TestClass]
public sealed class BootstrapAdminTests
{
    // Kiểm tra khởi tạo ba vai trò và ADMIN có mật khẩu băm, không ghi đè ADMIN hiện có.
    [TestMethod]
    public async Task Bootstrap_CreatesThreeRolesAndHashedAdmin_WithoutOverwritingExistingAdmin()
    {
        await using var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new BootstrapAdminService(db);
        var request = new CreateUserRequest("firstadmin", "First Admin", null, "Bootstrap123!", "Bootstrap123!", RoleNames.Admin);
        Assert.IsTrue(await service.CreateFirstAdminAsync(request));
        Assert.AreEqual(3, await db.Roles.CountAsync());
        var admin = await db.Users.SingleAsync();
        Assert.IsTrue(new PasswordHasher().VerifyPassword(request.Password, admin.PasswordHash));
        Assert.AreEqual(1, await db.SystemLogs.CountAsync());
        Assert.IsFalse(await service.CreateFirstAdminAsync(request with { Username = "secondadmin" }));
        Assert.AreEqual(1, await db.Users.CountAsync());
        Assert.AreEqual(1, await db.SystemLogs.CountAsync());
    }

    // Kiểm tra mật khẩu khởi tạo không hợp lệ bị chặn trước khi ghi dữ liệu.
    [TestMethod]
    public async Task Bootstrap_RejectsInvalidPasswordBeforeWriting()
    {
        await using var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => new BootstrapAdminService(db).CreateFirstAdminAsync(new CreateUserRequest("admin", "Admin", null, "short", "short", RoleNames.Admin)));
        Assert.AreEqual(0, await db.Roles.CountAsync());
        Assert.AreEqual(0, await db.Users.CountAsync());
    }
}
