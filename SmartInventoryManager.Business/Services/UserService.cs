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

    // Khởi tạo service xử lý tài khoản người dùng cùng các thành phần phụ thuộc.
    public UserService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    // Lấy danh sách tài khoản người dùng theo quyền và điều kiện lọc.
    public async Task<IReadOnlyList<UserListItemDto>> GetListAsync(UserSession session, string? search = null, string? roleName = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = await QueryListAsync(session, search, roleName, isActive, cancellationToken);
        return await query.ToListAsync(cancellationToken);
    }

    // Lấy một trang tài khoản người dùng theo bộ lọc và phạm vi truy cập.
    public async Task<PagedResultDto<UserListItemDto>> GetPageAsync(UserSession session, int pageNumber = 1, int pageSize = 50, string? search = null, string? roleName = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = await QueryListAsync(session, search, roleName, isActive, cancellationToken);
        return await QueryPaging.ReadAsync(query, pageNumber, pageSize, cancellationToken);
    }

    // Tạo truy vấn tài khoản người dùng với các điều kiện lọc và giới hạn quyền.
    private async Task<IQueryable<UserListItemDto>> QueryListAsync(UserSession session, string? search, string? roleName, bool? isActive, CancellationToken cancellationToken)
    {
        EnsureAdmin(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
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

        return query.OrderBy(user => user.Username)
            .Select(user => new UserListItemDto(user.UserId, user.Username, user.FullName, user.Email,
                user.Role.RoleName, user.IsActive, user.CreatedAt));
    }

    // Kiểm tra dữ liệu rồi tạo mới tài khoản người dùng.
    public async Task<int> CreateAsync(
        UserSession session,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken) : null;
        EnsureAdmin(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var username = request.Username.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        await UserIdentifiers.EnsureAvailableAsync(_dbContext, username, email, 0, cancellationToken);

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
        RecordUserAction(session, "USER_CREATED", null);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return user.UserId;
    }

    // Ngừng hoạt động tài khoản khi điều kiện bảo vệ tài khoản cho phép.
    public async Task DeactivateAsync(
        UserSession session,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken) : null;
        EnsureAdmin(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        var currentUserId = RequireCurrentUser(session);
        if (currentUserId == userId)
            throw new InvalidOperationException("Bạn không thể tự vô hiệu hóa tài khoản đang đăng nhập.");

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        await _dbContext.Entry(user).ReloadAsync(cancellationToken);

        if (!user.IsActive)
            throw new InvalidOperationException("Tài khoản đã được vô hiệu hóa.");

        await EnsureRoleChangeAllowedAsync(user, string.Empty, false, cancellationToken);
        user.IsActive = false;
        RecordUserAction(session, "USER_DEACTIVATED", userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    // Cập nhật hồ sơ, vai trò và trạng thái tài khoản theo quyền quản trị.
    public async Task UpdateUserAsync(
        UserSession session,
        int userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken) : null;
        EnsureAdmin(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 100)
            throw new ArgumentException("Họ tên là bắt buộc và không quá 100 ký tự.", nameof(request));

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (email is not null && !MailAddress.TryCreate(email, out _))
            throw new ArgumentException("Email không đúng định dạng.", nameof(request));

        var roleName = NormalizeRole(request.RoleName);
        var currentUserId = RequireCurrentUser(session);
        if (currentUserId == userId && !request.IsActive)
            throw new InvalidOperationException("Bạn không thể tự vô hiệu hóa tài khoản đang đăng nhập.");
        var user = await _dbContext.Users.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        await _dbContext.Entry(user).ReloadAsync(cancellationToken);
        await UserIdentifiers.EnsureAvailableAsync(_dbContext, user.Username, email, userId, cancellationToken);

        var role = await _dbContext.Roles.SingleOrDefaultAsync(
                item => item.RoleName == roleName, cancellationToken)
            ?? throw new InvalidOperationException("Vai trò không tồn tại trong hệ thống.");

        await EnsureRoleChangeAllowedAsync(user, roleName, request.IsActive, cancellationToken);
        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.RoleId = role.RoleId;
        user.IsActive = request.IsActive;
        RecordUserAction(session, "USER_UPDATED", userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    // Đặt lại mật khẩu tài khoản theo quyền quản trị viên.
    public async Task ResetPasswordAsync(
        UserSession session,
        int userId,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ArgumentException("Mật khẩu mới phải có ít nhất 8 ký tự.", nameof(request));
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            throw new ArgumentException("Mật khẩu xác nhận không khớp.", nameof(request));

        var user = await _dbContext.Users.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        RecordUserAction(session, "PASSWORD_RESET", userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Cập nhật thông tin cá nhân của tài khoản đang đăng nhập.
    public async Task UpdateAccountAsync(UserSession session, UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken) : null;
        var userId = RequireCurrentUser(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 100)
            throw new ArgumentException("Họ tên là bắt buộc và không quá 100 ký tự.", nameof(request));
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (email is not null && !MailAddress.TryCreate(email, out _))
            throw new ArgumentException("Email không đúng định dạng.", nameof(request));
        var user = await _dbContext.Users.SingleAsync(item => item.UserId == userId, cancellationToken);
        await _dbContext.Entry(user).ReloadAsync(cancellationToken);
        await UserIdentifiers.EnsureAvailableAsync(_dbContext, user.Username, email, userId, cancellationToken);
        user.FullName = request.FullName.Trim();
        user.Email = email;
        RecordUserAction(session, "ACCOUNT_UPDATED", userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    // Kiểm tra mật khẩu hiện tại rồi đổi mật khẩu cho tài khoản đang đăng nhập.
    public async Task ChangePasswordAsync(UserSession session, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireCurrentUser(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ArgumentException("Mật khẩu mới phải có ít nhất 8 ký tự.", nameof(request));
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            throw new ArgumentException("Mật khẩu xác nhận không khớp.", nameof(request));
        var user = await _dbContext.Users.SingleAsync(item => item.UserId == userId, cancellationToken);
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không đúng.");
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        RecordUserAction(session, "PASSWORD_CHANGED", userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // Chặn đổi vai trò hoặc ngừng tài khoản còn phụ trách kho hay là ADMIN cuối cùng.
    private async Task EnsureRoleChangeAllowedAsync(User user, string nextRole, bool nextActive, CancellationToken ct)
    {
        if ((!nextActive || nextRole != RoleNames.WarehouseManager)
            && await _dbContext.Warehouses.AsNoTracking().AnyAsync(warehouse => warehouse.ManagerId == user.UserId, ct))
            throw new InvalidOperationException("Hãy đổi hoặc gỡ người quản lý tại các kho trước khi đổi vai trò hoặc vô hiệu hóa tài khoản này.");
        var wasAdmin = user.IsActive && await _dbContext.Roles.AnyAsync(role => role.RoleId == user.RoleId && role.RoleName == RoleNames.Admin, ct);
        if (wasAdmin && (!nextActive || nextRole != RoleNames.Admin)
            && !await _dbContext.Users.AsNoTracking().AnyAsync(other => other.UserId != user.UserId && other.IsActive && other.Role.RoleName == RoleNames.Admin, ct))
            throw new InvalidOperationException("Hệ thống phải còn ít nhất một ADMIN đang hoạt động.");
    }

    // Thêm nhật ký cho thao tác quản lý tài khoản.
    private void RecordUserAction(UserSession session, string action, int? targetUserId)
    {
        _dbContext.SystemLogs.Add(new SystemLog
        {
            UserId = RequireCurrentUser(session),
            Action = action,
            EntityName = "User",
            EntityId = targetUserId,
            Description = "User account operation; credentials are never recorded.",
            CreatedAt = DateTime.Now
        });
    }
    // Yêu cầu phiên đăng nhập có quyền quản trị viên.
    private void EnsureAdmin(UserSession session)
    {
        if (!_authorization.CanManageUser(session))
            throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý tài khoản.");
    }

    // Kiểm tra dữ liệu yêu cầu xử lý tài khoản người dùng trước khi lưu.
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

    // Chuẩn hóa tên vai trò về giá trị được hệ thống hỗ trợ.
    private static string NormalizeRole(string roleName)
    {
        var normalized = roleName.Trim().ToUpperInvariant();
        return normalized switch
        {
            RoleNames.Admin or RoleNames.WarehouseManager or RoleNames.Staff => normalized,
            _ => throw new ArgumentException("Vai trò không hợp lệ.", nameof(roleName))
        };
    }

    // Lấy tài khoản hiện tại sau khi kiểm tra phiên đăng nhập.
    private static int RequireCurrentUser(UserSession session) => session.CurrentUser?.UserId
        ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để cập nhật tài khoản.");
}
