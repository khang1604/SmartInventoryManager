using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Data.Queries;

public sealed record DailyMovementQuantity(DateTime Date, long ImportQuantity, long ExportQuantity);

public sealed class DashboardQueries(InventoryDbContext db)
{
    // Truy vấn tổng số lượng nhập xuất theo ngày trong phạm vi kho cho phép.
    public async Task<IReadOnlyList<DailyMovementQuantity>> GetDailyAsync(int[] warehouseIds, DateTime from, DateTime toExclusive, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
        {
            var rows = await db.InventoryTransactions.AsNoTracking().Where(item => warehouseIds.Contains(item.WarehouseId)
                && item.CreatedAt >= from && item.CreatedAt < toExclusive && (item.TransactionType == "IMPORT" || item.TransactionType == "EXPORT")).ToListAsync(ct);
            return rows.GroupBy(item => item.CreatedAt.Date).OrderBy(group => group.Key)
                .Select(group => new DailyMovementQuantity(group.Key, group.Where(item => item.TransactionType == "IMPORT").Sum(item => (long)item.Quantity),
                    group.Where(item => item.TransactionType == "EXPORT").Sum(item => (long)item.Quantity))).ToList();
        }
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = (SqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = """
                SELECT CAST(CreatedAt AS date) AS MovementDate,
                    SUM(CASE WHEN TransactionType = 'IMPORT' THEN CAST(Quantity AS bigint) ELSE 0 END),
                    SUM(CASE WHEN TransactionType = 'EXPORT' THEN CAST(Quantity AS bigint) ELSE 0 END)
                FROM InventoryTransactions
                WHERE WarehouseId IN (SELECT CAST([value] AS int) FROM OPENJSON(@warehouses))
                    AND CreatedAt >= @from AND CreatedAt < @to
                    AND TransactionType IN ('IMPORT', 'EXPORT')
                GROUP BY CAST(CreatedAt AS date) ORDER BY MovementDate
                """;
            command.Parameters.Add("@warehouses", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(warehouseIds);
            command.Parameters.Add("@from", SqlDbType.DateTime2).Value = from;
            command.Parameters.Add("@to", SqlDbType.DateTime2).Value = toExclusive;
            var result = new List<DailyMovementQuantity>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) result.Add(new(reader.GetDateTime(0), reader.GetInt64(1), reader.GetInt64(2)));
            return result;
        }
        finally { if (opened) await connection.CloseAsync(); }
    }
}
