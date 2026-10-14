using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Common.Security;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

// Explicit setup operation; never registered in the desktop UI or run at application startup.
public sealed class BootstrapAdminService(InventoryDbContext db)
{
    public Task<bool> HasActiveAdminAsync(CancellationToken ct = default) => db.Users.AnyAsync(user => user.IsActive && user.Role.RoleName == RoleNames.Admin, ct);

    public async Task<bool> CreateFirstAdminAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length > 50 || string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 100)
            throw new ArgumentException("Tên đăng nhập và họ tên là bắt buộc, đúng giới hạn 50/100 ký tự.");
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8 || request.Password != request.ConfirmPassword)
            throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự và xác nhận trùng khớp.");
        if (!string.IsNullOrWhiteSpace(request.Email) && (request.Email.Trim().Length > 100 || !MailAddress.TryCreate(request.Email.Trim(), out _)))
            throw new ArgumentException("Email không hợp lệ.");
        if (request.RoleName != RoleNames.Admin || !request.IsActive) throw new ArgumentException("Bootstrap chỉ tạo ADMIN hoạt động.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
        if (await HasActiveAdminAsync(ct)) return false;
        var name = request.Username.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (await db.Users.AnyAsync(user => user.Username == name || (email != null && user.Email == email), ct))
            throw new InvalidOperationException("Tên đăng nhập/email đã tồn tại; không ghi đè tài khoản.");
        var names = new[] { RoleNames.Admin, RoleNames.WarehouseManager, RoleNames.Staff };
        var roles = await db.Roles.ToListAsync(ct);
        if (roles.Any(role => !names.Contains(role.RoleName))) throw new InvalidOperationException("Có role ngoài hợp đồng; cần kiểm tra trước bootstrap.");
        foreach (var roleName in names.Where(roleName => roles.All(role => role.RoleName != roleName)))
        {
            var role = new Role { RoleName = roleName }; db.Roles.Add(role); roles.Add(role);
        }
        var admin = new User { Username = name, FullName = request.FullName.Trim(), Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Role = roles.Single(role => role.RoleName == RoleNames.Admin), PasswordHash = new PasswordHasher().HashPassword(request.Password), IsActive = true, CreatedAt = DateTime.Now };
        db.Users.Add(admin); await db.SaveChangesAsync(ct);
        db.SystemLogs.Add(new SystemLog { UserId = admin.UserId, EntityName = "User", EntityId = admin.UserId, Action = "BOOTSTRAP_ADMIN", Description = "Đã khởi tạo quản trị viên đầu tiên.", CreatedAt = DateTime.Now });
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return true;
    }
}
