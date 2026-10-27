using Microsoft.Data.SqlClient;

namespace SmartInventoryManager.Data.Queries;

public static class DatabaseConnectionProbe
{
    // Read-only connection check. Never migrate, bootstrap or grant permissions here.
    // Kiểm tra kết nối và khả năng truy vấn mà không thay đổi cấu trúc hay dữ liệu.
    public static async Task CheckAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 8;
        command.CommandText = "SELECT TOP (0) UserId FROM dbo.Users; SELECT TOP (0) RoleName FROM dbo.Roles; SELECT TOP (0) Quantity FROM dbo.Inventory;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.NextResultAsync(cancellationToken)) { }
    }
}
