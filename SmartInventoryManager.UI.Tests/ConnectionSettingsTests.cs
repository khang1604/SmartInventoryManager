using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.UI.Tests;

[TestClass]
public sealed class ConnectionSettingsTests
{
    private string _directory = null!;
    private string FilePath => Path.Combine(_directory, "connection.dat");

    [TestInitialize]
    public void Initialize()
    {
        var root = Path.GetDirectoryName(typeof(ConnectionSettingsTests).Assembly.Location)!;
        while (!File.Exists(Path.Combine(root, "SmartInventoryManager.slnx")))
            root = Directory.GetParent(root)?.FullName ?? throw new InvalidOperationException("Workspace root not found.");
        _directory = Path.Combine(root, ".temp", "ConnectionSettingsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Only files created by this test, never user application data.
        foreach (var name in new[] { "connection.dat", "connection.dat.tmp" })
            File.Delete(Path.Combine(_directory, name));
        Directory.Delete(_directory);
    }

    [TestMethod]
    public void PasswordWithDelimitersCannotChangeConnectionTarget()
    {
        var password = "FixtureSecret;Server=evil;Password='\"";
        var profile = new ConnectionProfile("100.64.0.1", 14330, "Fixture DB", "test-user", password);
        var builder = new SqlConnectionStringBuilder(profile.BuildConnectionString());
        Assert.AreEqual("tcp:100.64.0.1,14330", builder.DataSource);
        Assert.AreEqual(password, builder.Password);
        Assert.IsFalse(builder.TrustServerCertificate);
        Assert.AreEqual(SqlConnectionEncryptOption.Mandatory, builder.Encrypt);
        Assert.IsFalse(profile.ToString().Contains(password));
    }

    [TestMethod]
    public void InvalidServerOrMissingCredentialsAreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => new ConnectionProfile("host;Encrypt=False", 14330, "DB", "user", "secret").BuildConnectionString());
        Assert.ThrowsException<ArgumentException>(() => new ConnectionProfile("host", 0, "DB", "user", "secret").BuildConnectionString());
        Assert.ThrowsException<ArgumentException>(() => new ConnectionProfile("host", 14330, "DB", "user", "").BuildConnectionString());
    }

    [TestMethod]
    public void WindowsAuthenticationDoesNotKeepSqlPassword()
    {
        var builder = new SqlConnectionStringBuilder(new ConnectionProfile("localhost", 14330, "DB", "unused", "unused", true).BuildConnectionString());
        Assert.IsTrue(builder.IntegratedSecurity);
        Assert.AreEqual(string.Empty, builder.Password);
        Assert.AreEqual(string.Empty, builder.UserID);
    }

    [TestMethod]
    public void DpapiProfileReloadsWithoutPlaintextSecretAndSupportsReplacement()
    {
        var store = new ConnectionSettingsStore(FilePath);
        var secret = "Fixture-only-long-secret-for-DPAPI";
        var first = new ConnectionProfile("localhost", 14330, "Fixture", "user", secret).BuildConnectionString();
        store.Save(first);
        Assert.IsFalse(Encoding.UTF8.GetString(File.ReadAllBytes(FilePath)).Contains(secret));
        Assert.AreEqual(first, store.Load(out var unreadable)); Assert.IsFalse(unreadable);
        var next = new ConnectionProfile("localhost", 14330, "OtherFixture", "user", secret).BuildConnectionString();
        store.Save(next);
        Assert.AreEqual(next, store.Load(out unreadable)); Assert.IsFalse(unreadable);
        Assert.IsFalse(File.Exists(FilePath + ".tmp"));
    }

    [TestMethod]
    public void MissingOrTamperedProfileReturnsSetupInsteadOfThrowing()
    {
        var store = new ConnectionSettingsStore(FilePath);
        Assert.IsNull(store.Load(out var unreadable)); Assert.IsFalse(unreadable);
        store.Save(new ConnectionProfile("localhost", 14330, "Fixture", "user", "fixture").BuildConnectionString());
        var bytes = File.ReadAllBytes(FilePath); bytes[^1] ^= 1; File.WriteAllBytes(FilePath, bytes);
        Assert.IsNull(store.Load(out unreadable)); Assert.IsTrue(unreadable);
    }
}
