using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Services;
using SmartInventoryManager.Data.Context;

Console.OutputEncoding = Encoding.UTF8;
try
{
    var connection = Environment.GetEnvironmentVariable("SMART_INVENTORY_CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Cần cấu hình SMART_INVENTORY_CONNECTION_STRING cho đúng DB đích.");
    var target = new SqlConnectionStringBuilder(connection);
    if (string.IsNullOrWhiteSpace(target.InitialCatalog) || new[] { "master", "model", "msdb", "tempdb" }.Contains(target.InitialCatalog.ToLowerInvariant()))
        throw new InvalidOperationException("Không dùng database hệ thống để bootstrap.");
    Console.WriteLine($"Đích: {target.DataSource} / {target.InitialCatalog}");
    await using var db = new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlServer(connection).Options);
    if (!await db.Database.CanConnectAsync()) throw new InvalidOperationException("Không kết nối được DB đích.");
    if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Cần apply migration trước; công cụ này không tự tạo schema.");
    var bootstrap = new BootstrapAdminService(db);
    if (await bootstrap.HasActiveAdminAsync()) { Console.WriteLine("Đã có ADMIN hoạt động; không tạo hoặc ghi đè tài khoản."); return; }
    if (args.Contains("--check")) { Console.WriteLine("Chưa có ADMIN hoạt động; có thể chạy bootstrap tương tác."); return; }
    if (Console.IsInputRedirected) throw new InvalidOperationException("Bootstrap yêu cầu terminal tương tác để nhập mật khẩu an toàn.");
    Console.Write("Xác nhận DB đích (nhập chính xác tên database): ");
    if (Console.ReadLine() != target.InitialCatalog) throw new InvalidOperationException("Tên DB không khớp; dừng trước khi ghi.");
    Console.Write("Tên đăng nhập: "); var username = Console.ReadLine() ?? "";
    Console.Write("Họ tên: "); var fullName = Console.ReadLine() ?? "";
    Console.Write("Email (có thể bỏ trống): "); var email = Console.ReadLine();
    var password = ReadPassword("Mật khẩu: "); var confirm = ReadPassword("Xác nhận mật khẩu: ");
    var created = await bootstrap.CreateFirstAdminAsync(new CreateUserRequest(username, fullName, email, password, confirm, RoleNames.Admin, true));
    Console.WriteLine(created ? "Đã tạo ADMIN đầu tiên và đủ ba role; mật khẩu lưu dạng PBKDF2." : "ADMIN đã được khởi tạo bởi thao tác khác; không ghi đè.");
}
catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(ex.Message); Environment.ExitCode = 1;
}
catch (Exception)
{
    Console.Error.WriteLine("Không hoàn tất bootstrap. Kiểm tra connection, migration và quyền DB; không tự chạy lại khi chưa kiểm tra kết quả."); Environment.ExitCode = 1;
}

static string ReadPassword(string prompt)
{
    Console.Write(prompt); var text = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return text.ToString(); }
        if (key.Key == ConsoleKey.Backspace) { if (text.Length > 0) { text.Length--; Console.Write("\b \b"); } }
        else if (!char.IsControl(key.KeyChar)) { text.Append(key.KeyChar); Console.Write('*'); }
    }
}
