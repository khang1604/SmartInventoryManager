using Microsoft.Data.SqlClient;

namespace SmartInventoryManager.UI.Components;

internal sealed record ConnectionProfile(string Server, int Port, string Database, string Username,
    string Password, bool WindowsAuthentication = false, bool TrustServerCertificate = false)
{
    // Tạo tên hiển thị mô tả máy chủ và cơ sở dữ liệu trong cấu hình.
    public override string ToString() => $"{Server}:{Port}/{Database}";

    // Kiểm tra cấu hình và tạo chuỗi kết nối SQL Server bằng builder.
    public string BuildConnectionString()
    {
        if (string.IsNullOrWhiteSpace(Server) || Server.IndexOfAny([';', '\r', '\n', ',', '\\']) >= 0
            || Port is < 1 or > 65535 || string.IsNullOrWhiteSpace(Database)
            || (!WindowsAuthentication && (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))))
            throw new ArgumentException("Nhập địa chỉ máy chủ, tên dữ liệu và thông tin kết nối. Nhập cổng ở ô riêng.");
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"tcp:{Server.Trim()},{Port}", InitialCatalog = Database.Trim(),
            IntegratedSecurity = WindowsAuthentication, Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = TrustServerCertificate, MultipleActiveResultSets = true,
            ConnectTimeout = 8, PersistSecurityInfo = false, ApplicationName = "VioWare"
        };
        if (!WindowsAuthentication) { builder.UserID = Username.Trim(); builder.Password = Password; }
        return builder.ConnectionString;
    }

    // Phân tích chuỗi kết nối thành cấu hình phục vụ giao diện thiết lập.
    public static ConnectionProfile FromConnectionString(string value)
    {
        var builder = new SqlConnectionStringBuilder(value);
        var address = builder.DataSource.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)
            ? builder.DataSource[4..] : builder.DataSource;
        var parts = address.Split(',');
        return new(parts[0], parts.Length == 2 && int.TryParse(parts[1], out var port) ? port : 14330,
            builder.InitialCatalog, builder.UserID, builder.Password, builder.IntegratedSecurity, builder.TrustServerCertificate);
    }
}
