using Microsoft.Data.SqlClient;

namespace SmartInventoryManager.Business.Services;

public sealed class BusinessConflictException(string code, string message, Exception inner) : InvalidOperationException(message, inner)
{
    public string Code { get; } = code;
}

internal static class DeadlockRetry
{
    internal static async Task RunAsync(Func<Task> operation, Action resetTracking, Func<bool> hasCommitted, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { await operation(); return; }
            catch (Exception ex) when (!hasCommitted() && ex.GetBaseException() is SqlException { Number: 1205 })
            {
                // SQL Server deadlock victims have their entire transaction rolled back.
                resetTracking();
                if (attempt >= 2)
                    throw new BusinessConflictException("CONCURRENCY_RETRY_EXHAUSTED", "Dữ liệu đang được xử lý đồng thời. Vui lòng làm mới trước khi thử lại.", ex);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), ct);
            }
        }
    }
}
