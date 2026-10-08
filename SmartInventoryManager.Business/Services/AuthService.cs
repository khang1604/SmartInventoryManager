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

        public AuthService(InventoryDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

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

            var normalizedIdentifier = username!.Trim();
            var user = await _dbContext.Users
                .AsNoTracking()
                .Include(candidate => candidate.Role)
                .SingleOrDefaultAsync(
                    candidate => candidate.Email == normalizedIdentifier ||
                        candidate.Username == normalizedIdentifier,
                    cancellationToken);

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

        public string HashPassword(string password) => _passwordHasher.HashPassword(password);

        public bool VerifyPassword(string? password, string? storedPasswordHash) =>
            _passwordHasher.VerifyPassword(password, storedPasswordHash);

        public bool HasRole(string? currentRole, string? requiredRole)
        {
            return !string.IsNullOrWhiteSpace(currentRole) &&
                !string.IsNullOrWhiteSpace(requiredRole) &&
                string.Equals(
                    currentRole.Trim(),
                    requiredRole.Trim(),
                    StringComparison.OrdinalIgnoreCase);
        }

        public bool HasAnyRole(string? currentRole, params string[] allowedRoles)
        {
            if (string.IsNullOrWhiteSpace(currentRole))
            {
                return false;
            }

            return allowedRoles.Any(role => HasRole(currentRole, role));
        }

        public bool IsKnownRole(string? role)
        {
            return HasAnyRole(
                role,
                RoleNames.Admin,
                RoleNames.WarehouseManager,
                RoleNames.Staff);
        }

        private static bool IsValidEmail(string email)
        {
            var normalizedEmail = email.Trim();
            return MailAddress.TryCreate(normalizedEmail, out var address) &&
                address is not null &&
                string.Equals(address.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
