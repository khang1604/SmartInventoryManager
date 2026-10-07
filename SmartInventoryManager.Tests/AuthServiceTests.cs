using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Common.Security;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Tests;

[TestClass]
public class AuthServiceTests
{
    [TestMethod]
    public void ValidateLoginCredentials_AcceptsUsernameOrEmailIdentifier()
    {
        var service = CreateAuthService();

        var usernameErrors = service.ValidateLoginCredentials("admin", "SmartInventoryManager");
        var emailErrors = service.ValidateLoginCredentials("admin@smartinventory.local", "SmartInventoryManager");

        Assert.AreEqual(0, usernameErrors.Count);
        Assert.AreEqual(0, emailErrors.Count);
    }

    [TestMethod]
    public void ValidateLoginCredentials_RejectsMissingPassword()
    {
        var errors = CreateAuthService().ValidateLoginCredentials("admin", null);

        CollectionAssert.Contains(errors.ToList(), "Vui lòng nhập mật khẩu.");
    }

    [TestMethod]
    public void RoleHelpers_AreCaseInsensitiveAndTrimmed()
    {
        var service = CreateAuthService();

        Assert.IsTrue(service.HasRole(" admin ", RoleNames.Admin));
        Assert.IsTrue(service.HasAnyRole("STAFF", RoleNames.Admin, RoleNames.Staff));
        Assert.IsTrue(service.IsKnownRole(" warehouse_manager "));
        Assert.IsFalse(service.IsKnownRole("UNKNOWN"));
    }

    [TestMethod]
    public void PasswordHasher_CanVerifyGeneratedHash()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("SmartInventoryManager");

        Assert.IsTrue(hasher.VerifyPassword("SmartInventoryManager", hash));
        Assert.IsFalse(hasher.VerifyPassword("wrong-password", hash));
    }

    [TestMethod]
    public void PasswordHasher_RejectsMalformedHash()
    {
        var hasher = new PasswordHasher();

        Assert.IsFalse(hasher.VerifyPassword("SmartInventoryManager", "not-a-password-hash"));
        Assert.IsFalse(hasher.VerifyPassword(null, null));
    }

    private static AuthService CreateAuthService()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>().Options;
        return new AuthService(new InventoryDbContext(options));
    }
}
