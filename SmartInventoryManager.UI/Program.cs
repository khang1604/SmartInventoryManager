using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.UI.Forms.Main;

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

            // Điều hướng UI dùng kết quả xác thực có sẵn, không thay đổi nghiệp vụ đăng nhập.
            
        }
    }
}
