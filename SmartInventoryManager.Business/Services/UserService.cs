using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Common.Security;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class UserService
{
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly PasswordHasher _passwordHasher = new();

    public UserService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetListAsync(
        UserSession session,
        string? search = null,
        string? roleName = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(session);
        var query = _dbContext.Users.AsNoTracking().Include(user => user.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(user => user.Username.Contains(value) || user.FullName.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var normalizedRole = NormalizeRole(roleName);
            query = query.Where(user => user.Role.RoleName == normalizedRole);
        }
        if (isActive.HasValue)
            query = query.Where(user => user.IsActive == isActive.Value);

        return await query.OrderBy(user => user.Username)
            .Select(user => new UserListItemDto(user.UserId, user.Username, user.FullName, user.Email,
                user.Role.RoleName, user.IsActive, user.CreatedAt))
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(
        UserSession session,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(session);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var username = request.Username.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (await _dbContext.Users.AnyAsync(user => user.Username == username, cancellationToken))
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
        if (email is not null && await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new InvalidOperationException("Email đã được sử dụng.");

        var roleName = NormalizeRole(request.RoleName);
        var role = await _dbContext.Roles.SingleOrDefaultAsync(item => item.RoleName == roleName, cancellationToken)
            ?? throw new InvalidOperationException("Vai trò không tồn tại trong hệ thống.");
        var user = new User
        {
            Username = username,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            RoleId = role.RoleId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.UserId;
    }

    private void EnsureAdmin(UserSession session)
    {
        if (!_authorization.CanManageUser(session))
            throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý tài khoản.");
    }

    private static void ValidateRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length > 50)
            throw new ArgumentException("Tên đăng nhập là bắt buộc và không quá 50 ký tự.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 100)
            throw new ArgumentException("Họ tên là bắt buộc và không quá 100 ký tự.", nameof(request));
        if (!string.IsNullOrWhiteSpace(request.Email) && !MailAddress.TryCreate(request.Email.Trim(), out _))
            throw new ArgumentException("Email không đúng định dạng.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự.", nameof(request));
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            throw new ArgumentException("Mật khẩu xác nhận không khớp.", nameof(request));
        _ = NormalizeRole(request.RoleName);
    }

    private static string NormalizeRole(string roleName)
    {
        var normalized = roleName.Trim().ToUpperInvariant();
        return normalized switch
        {
            RoleNames.Admin or RoleNames.WarehouseManager or RoleNames.Staff => normalized,
            _ => throw new ArgumentException("Vai trò không hợp lệ.", nameof(roleName))
        };
    }
}
