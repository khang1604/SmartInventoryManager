using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Events;

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

    [TestMethod]
    public void StockAlertEvent_PreservesResolutionPayload()
    {
        var timestamp = new DateTime(2026, 10, 8, 12, 30, 0);
        var alert = new StockAlertEvent(4, 9, "LOW_STOCK", 27, 3, timestamp);

        Assert.AreEqual(4, alert.ProductId);
        Assert.AreEqual(9, alert.WarehouseId);
        Assert.AreEqual("LOW_STOCK", alert.AlertType);
        Assert.AreEqual(27, alert.AlertId);
        Assert.AreEqual(3, alert.CurrentQuantity);
        Assert.AreEqual(timestamp, alert.Timestamp);
    }
}
