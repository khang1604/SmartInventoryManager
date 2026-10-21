using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.UI.Forms.Login;
using SmartInventoryManager.UI.Forms.Main;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Forms.Settings;
using SmartInventoryManager.Data.Queries;

namespace SmartInventoryManager.UI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var store = new ConnectionSettingsStore();
            if (args.Contains("--configure", StringComparer.OrdinalIgnoreCase))
            {
                using var settings = new FrmConnectionSettings(store, DatabaseConnectionProbe.CheckAsync);
                settings.ShowDialog();
                return;
            }
            while (true)
            {
                var connectionString = ResolveConnectionString(store);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    using var settings = new FrmConnectionSettings(store, DatabaseConnectionProbe.CheckAsync);
                    if (settings.ShowDialog() != DialogResult.OK) return;
                    connectionString = settings.ConnectionString!;
                }
                if (!RunApplication(connectionString)) return;
                using var configure = new FrmConnectionSettings(store, DatabaseConnectionProbe.CheckAsync);
                configure.ShowDialog();
            }
        }

        private static string? ResolveConnectionString(ConnectionSettingsStore store)
        {
            var connectionString = Environment.GetEnvironmentVariable(
                "SMART_INVENTORY_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString)) return connectionString;
            connectionString = store.Load(out var unreadable);
            // An unreadable local profile must return to setup, not silently switch databases.
            return unreadable ? null : connectionString ?? LoadConnectionStringFromAppConfig();
        }

        private static bool RunApplication(string connectionString)
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var dbContext = new InventoryDbContext(options);
            var authService = new AuthService(dbContext);

            var authorizationService = new AuthorizationService(dbContext);
            var session = new UserSession();
            var stockPublisher = new SmartInventoryManager.Business.Events.StockEventPublisher();
            using var services = new UiServices(session, authorizationService,
                new ImportService(dbContext, authorizationService, stockPublisher),
                new ExportService(dbContext, authorizationService, stockPublisher),
                new InventoryService(dbContext, authorizationService, stockPublisher),
                new StockAlertService(dbContext, authorizationService, stockPublisher),
                new ProductService(dbContext), new WarehouseService(dbContext, authorizationService),
                 new SupplierService(dbContext, authorizationService),
                new CategoryService(dbContext, authorizationService),
                new ProductManagementService(dbContext, authorizationService, stockPublisher),
                new LocationService(dbContext, authorizationService),
                new DashboardService(dbContext, authorizationService),
                new ReportService(dbContext, authorizationService),
                new UserService(dbContext, authorizationService),
                new NotificationService(dbContext),
                () => dbContext.ChangeTracker.Clear(), new WarehouseAssignmentService(dbContext, authorizationService), new SystemLogService(dbContext, authorizationService),
                new AnalyticsService(dbContext, authorizationService), new ReceiptAuditService(dbContext, authorizationService));
            while (true)
            {
                using var loginForm = new FrmLogin(authService);
                var loginResult = loginForm.ShowDialog();
                if (loginResult != DialogResult.OK || loginForm.AuthenticatedUser is null)
                {
                    return false;
                }

                session.SignIn(loginForm.AuthenticatedUser);
                if (string.Equals(loginForm.AuthenticatedUser.RoleName.Trim(), RoleNames.Staff, StringComparison.OrdinalIgnoreCase))
                {
                    using var warehouseContext = new FrmWarehouseContext(services);
                    if (warehouseContext.ShowDialog() != DialogResult.OK)
                    {
                        session.SignOut();
                        continue;
                    }
                }
                using var mainForm = new FrmMain(loginForm.AuthenticatedUser, authorizationService, session, services);
                Application.Run(mainForm);
                session.SignOut();
                dbContext.ChangeTracker.Clear();
                if (!mainForm.LogoutRequested)
                {
                    return false;
                }
            }
        }

        private static string? LoadConnectionStringFromAppConfig()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "SmartInventoryManager.UI.dll.config"),
                Path.Combine(AppContext.BaseDirectory, "SmartInventoryManager.UI.exe.config"),
                Path.Combine(AppContext.BaseDirectory, "App.config"),
                Path.Combine(Directory.GetCurrentDirectory(), "App.config")
            };

            foreach (var candidate in candidates)
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                try
                {
                    var document = XDocument.Load(candidate);
                    var connectionString = document.Descendants("add")
                        .FirstOrDefault(node =>
                            string.Equals(node.Attribute("name")?.Value, "SmartInventoryManagerDb", StringComparison.OrdinalIgnoreCase))?
                        .Attribute("connectionString")?
                        .Value;

                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        return connectionString;
                    }
                }
                catch (Exception)
                {
                    // Ignore misconfigured XML and fallback to the next candidate.
                }
            }

            return null;
        }
    }
}

