using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Tests;

[TestClass]
public class StockAlertServiceTests
{
    [TestMethod]
    public void NewSession_IsUnauthenticated()
    {
        var session = new UserSession();

        Assert.IsFalse(session.IsAuthenticated);
        Assert.IsNull(session.CurrentUser);
    }

    [TestMethod]
    public void SignInAndSignOut_UpdateSessionState()
    {
        var session = new UserSession();
        var user = new AuthenticatedUserDto(1, "admin", "Admin", "admin@example.com", RoleNames.Admin);

        session.SignIn(user);

        Assert.IsTrue(session.IsAuthenticated);
        Assert.AreSame(user, session.CurrentUser);

        session.SignOut();

        Assert.IsFalse(session.IsAuthenticated);
        Assert.IsNull(session.CurrentUser);
    }

    [TestMethod]
    public void SignIn_RejectsNullUser()
    {
        var session = new UserSession();

        Assert.Throws<ArgumentNullException>(() => session.SignIn(null!));
    }
}
