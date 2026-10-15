using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services;

internal static class UserIdentifiers
{
    internal static async Task EnsureAvailableAsync(InventoryDbContext db, string username, string? email, int exceptUserId, CancellationToken ct)
    {
        var names = new[] { username.Trim().ToUpperInvariant(), email?.Trim().ToUpperInvariant() }
            .Where(value => !string.IsNullOrEmpty(value)).Cast<string>().Distinct().ToArray();
        if (await db.Users.AsNoTracking().AnyAsync(user => user.UserId != exceptUserId
            && (names.Contains(user.Username.ToUpper()) || (user.Email != null && names.Contains(user.Email.ToUpper()))), ct))
            throw new InvalidOperationException("Tên đăng nhập hoặc email trùng với thông tin đăng nhập của tài khoản khác.");
    }
}
