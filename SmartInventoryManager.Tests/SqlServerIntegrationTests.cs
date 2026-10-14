using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Tests;

[TestClass]
[TestCategory("SQLServer")]
public sealed class SqlServerIntegrationTests
{
    private static DbContextOptions<InventoryDbContext> Options()
    {
        var connection = Environment.GetEnvironmentVariable("SMART_INVENTORY_SQL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection) || Environment.GetEnvironmentVariable("SMART_INVENTORY_SQL_TESTS_ALLOW_MUTATION") != "1")
            Assert.Inconclusive("SQL integration tests require an explicitly authorized test connection and mutation opt-in.");
        var target = new SqlConnectionStringBuilder(connection);
        Assert.AreEqual("localhost", target.DataSource, "Only the currently authorized local test server is supported.");
        Assert.AreEqual("SmartInventoryManagerDB", target.InitialCatalog, "Unexpected test database; stop before writes.");
        return new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connection).Options;
    }

    private static async Task<UserSession> AdminAsync(InventoryDbContext db)
    {
        Assert.IsFalse((await db.Database.GetPendingMigrationsAsync()).Any(), "Apply the approved test migrations before integration tests.");
        var user = await db.Users.AsNoTracking().Include(item => item.Role).FirstAsync(item => item.IsActive && item.Role.RoleName == RoleNames.Admin);
        var session = new UserSession(); session.SignIn(new AuthenticatedUserDto(user.UserId, user.Username, user.FullName, user.Email, RoleNames.Admin));
        return session;
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task ReceiptPosting_UsesFreshContextsAndDoesNotPostTwice()
    {
        var options = Options();
        await using var db = new InventoryDbContext(options);
        await SqlReceiptFixture.RunAsync(options, await AdminAsync(db));
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task DeadlockRetry_RollsBackVictimAndCommitsEachEffectOnce()
    {
        var options = Options();
        await using var db = new InventoryDbContext(options);
        await AdminAsync(db);
        await SqlDeadlockFixture.RunAsync(options);
    }

    [TestMethod]
    public async Task Dashboard_AdoDailyTotalsMatchAuthorizedMovementSource()
    {
        await using var db = new InventoryDbContext(Options());
        var session = await AdminAsync(db);
        var authorization = new AuthorizationService(db);
        var ids = (await authorization.GetAccessibleWarehouseIdsAsync(session)).ToArray();
        var result = await new DashboardService(db, authorization).GetAsync(session);
        var from = DateTime.Today.AddDays(-29); var end = DateTime.Today.AddDays(1);
        var source = db.InventoryTransactions.Where(item => ids.Contains(item.WarehouseId) && item.CreatedAt >= from && item.CreatedAt < end);
        var imports = await source.Where(item => item.TransactionType == "IMPORT").SumAsync(item => (long)item.Quantity);
        var exports = await source.Where(item => item.TransactionType == "EXPORT").SumAsync(item => (long)item.Quantity);
        Assert.AreEqual(imports, result.DailyQuantities.Sum(item => item.ImportQuantity));
        Assert.AreEqual(exports, result.DailyQuantities.Sum(item => item.ExportQuantity));
        Assert.AreEqual(30, result.DailyQuantities.Count);
        Assert.AreEqual(result.InventoryLineCount, result.StockDistribution.Sum(item => item.Count));
    }

    [TestMethod]
    [Timeout(90000)]
    public async Task TeamDecisionWorkflows_TransferDifferentExportsCancelRaceAndInjectedRollback()
    {
        var options = Options(); await using var db = new InventoryDbContext(options);
        await SqlTeamDecisionFixture.RunAsync(options, await AdminAsync(db));
    }
}
