using System.Net.Mail;
using SmartInventoryManager.Common.Security;

namespace SmartInventoryManager.Business.Services
{
    public class AuthService
    {
        private readonly PasswordHasher _passwordHasher = new();

        public IReadOnlyList<string> ValidateLoginCredentials(string? email, string? password)
        {
            var errors = new List<string>();

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

            return errors.AsReadOnly();
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

        private static bool IsValidEmail(string email)
        {
            var normalizedEmail = email.Trim();
            return MailAddress.TryCreate(normalizedEmail, out var address) &&
                address is not null &&
                string.Equals(address.Address, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
