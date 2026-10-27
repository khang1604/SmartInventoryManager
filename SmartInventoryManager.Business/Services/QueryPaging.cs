using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.DTOs;

namespace SmartInventoryManager.Business.Services;

internal static class QueryPaging
{
    // Đếm kết quả và đọc một trang dữ liệu với thứ tự ổn định.
    internal static async Task<PagedResultDto<T>> ReadAsync<T>(IQueryable<T> query, int pageNumber, int pageSize, CancellationToken ct)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100 || pageNumber > int.MaxValue / pageSize)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Trang phải dương và kích thước trang từ 1 đến 100.");
        var total = await query.CountAsync(ct);
        pageNumber = Math.Min(pageNumber, Math.Max(1, (int)(((long)total + pageSize - 1) / pageSize)));
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items, pageNumber, pageSize, total);
    }
}
