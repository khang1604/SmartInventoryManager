using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Data.Queries;

public static class TransferReferenceAllocator
{
    private static int _inMemoryCounter;
    // Cấp mã tham chiếu chung cho hai biến động của một lần chuyển kho.
    public static async Task<int> NextAsync(InventoryDbContext db, CancellationToken ct)
    {
        // EF InMemory is used only by isolated unit tests; it has no SQL sequence support.
        if (!db.Database.IsRelational()) return Interlocked.Increment(ref _inMemoryCounter);
        var tx = db.Database.CurrentTransaction ?? throw new InvalidOperationException("Transfer reference requires the posting transaction.");
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = tx.GetDbTransaction();
        command.CommandText = "SELECT NEXT VALUE FOR dbo.TransferReferenceSequence";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }
}
