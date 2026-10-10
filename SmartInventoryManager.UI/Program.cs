using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.UI.Forms.Login;
using SmartInventoryManager.UI.Forms.Main;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.UI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var connectionString = Environment.GetEnvironmentVariable(
                "SMART_INVENTORY_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = LoadConnectionStringFromAppConfig();
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show(
                    "Chưa cấu hình chuỗi kết nối cơ sở dữ liệu. Vui lòng liên hệ quản trị viên.",
                    "Không thể khởi động",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using var dbContext = new InventoryDbContext(options);
            var authService = new AuthService(dbContext);

            var authorizationService = new AuthorizationService(dbContext);
            var session = new UserSession();
            using var services = new UiServices(session, authorizationService,
                new ImportService(dbContext, authorizationService),
                new ExportService(dbContext, authorizationService),
                new InventoryService(dbContext, authorizationService),
                new StockAlertService(dbContext, authorizationService),
                new ProductService(dbContext), new WarehouseService(dbContext, authorizationService),
                 new SupplierService(dbContext, authorizationService),
                new CategoryService(dbContext, authorizationService),
                new ProductManagementService(dbContext, authorizationService),
                new LocationService(dbContext, authorizationService),
                new DashboardService(dbContext, authorizationService),
                new ReportService(dbContext, authorizationService),
                new UserService(dbContext, authorizationService),
                new NotificationService(dbContext),
                () => dbContext.ChangeTracker.Clear());
            while (true)
            {
                using var loginForm = new FrmLogin(authService);
                if (loginForm.ShowDialog() != DialogResult.OK || loginForm.AuthenticatedUser is null)
                {
                    return;
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
                    return;
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

