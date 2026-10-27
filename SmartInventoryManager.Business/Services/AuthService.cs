using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Common.Security;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;

namespace SmartInventoryManager.Business.Services
{
    public class AuthService
    {
        private readonly InventoryDbContext _dbContext;
        private readonly PasswordHasher _passwordHasher = new();

        // Khởi tạo service xác thực sử dụng dữ liệu tài khoản trong DbContext.
        public AuthService(InventoryDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        // Kiểm tra tên đăng nhập hoặc email và mật khẩu đã được nhập.
        public IReadOnlyList<string> ValidateLoginCredentials(string? username, string? password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add("Vui lòng nhập tên đăng nhập.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Vui lòng nhập mật khẩu.");
            }

            return errors.AsReadOnly();
        }

        // Xác thực tài khoản hoạt động và đối chiếu mật khẩu đã băm.
        public async Task<AuthenticatedUserDto?> AuthenticateAsync(
            string? username,
            string? password,
            CancellationToken cancellationToken = default)
        {
            var errors = ValidateLoginCredentials(username, password);
            if (errors.Count > 0)
            {
                return null;
            }

            var normalizedIdentifier = username!.Trim().ToUpperInvariant();
            var candidates = await _dbContext.Users
                .AsNoTracking()
                .Include(candidate => candidate.Role)
                .Where(candidate => (candidate.Email != null && candidate.Email.ToUpper() == normalizedIdentifier)
                    || candidate.Username.ToUpper() == normalizedIdentifier)
                .Take(2).ToListAsync(cancellationToken);
            if (candidates.Count > 1)
                throw new InvalidOperationException("Thông tin đăng nhập bị trùng giữa các tài khoản. Vui lòng liên hệ ADMIN để kiểm tra.");
            var user = candidates.SingleOrDefault();

            if (user is null || !user.IsActive ||
                !_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            return new AuthenticatedUserDto(
                user.UserId,
                user.Username,
                user.FullName,
                user.Email,
                user.Role.RoleName);
        }

        // Kiểm tra thông tin đăng ký tài khoản trước khi tạo người dùng.
        public IReadOnlyList<string> ValidateSignup(string? fullName, string? email, string? password, string? confirmPassword)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                errors.Add("Vui lòng nhập họ tên.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("Vui lòng nhập email.");
            }
            else if (!IsValidEmail(email))
            {
                errors.Add("Email không đúng định dạng.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Vui lòng nhập mật khẩu.");
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                errors.Add("Vui lòng xác nhận mật khẩu.");
            }
            else if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                errors.Add("Mật khẩu xác nhận không khớp.");
            }

            return errors.AsReadOnly();
        }

        // Băm mật khẩu để lưu và sử dụng khi xác thực.
        public string HashPassword(string password) => _passwordHasher.HashPassword(password);

        // Đối chiếu mật khẩu nhập vào với chuỗi băm đã lưu.
        public bool VerifyPassword(string? password, string? storedPasswordHash) =>
            _passwordHasher.VerifyPassword(password, storedPasswordHash);

        // Kiểm tra người dùng có vai trò được yêu cầu.
        public bool HasRole(string? currentRole, string? requiredRole)
        {
            return !string.IsNullOrWhiteSpace(currentRole) &&
                !string.IsNullOrWhiteSpace(requiredRole) &&
                string.Equals(
                    currentRole.Trim(),
                    requiredRole.Trim(),
                    StringComparison.OrdinalIgnoreCase);
        }

        // Kiểm tra người dùng có ít nhất một vai trò trong danh sách cho phép.
        public bool HasAnyRole(string? currentRole, params string[] allowedRoles)
        {
            if (string.IsNullOrWhiteSpace(currentRole))
            {
                return false;
            }

            return allowedRoles.Any(role => HasRole(currentRole, role));
        }

        // Kiểm tra tên vai trò thuộc các vai trò được hệ thống hỗ trợ.
        public bool IsKnownRole(string? role)
        {
            return HasAnyRole(
                role,
                RoleNames.Admin,
                RoleNames.WarehouseManager,
                RoleNames.Staff);
        }

        // Kiểm tra chuỗi email có định dạng hợp lệ.
        private static bool IsValidEmail(string email)
        {
            var normalizedEmail = email.Trim();
            return MailAddress.TryCreate(normalizedEmail, out var address) &&
                address is not null &&
                string.Equals(address.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
