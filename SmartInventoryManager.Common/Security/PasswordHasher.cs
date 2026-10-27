using System.Globalization;
using System.Security.Cryptography;

namespace SmartInventoryManager.Common.Security;

public sealed class PasswordHasher
{
    public const int MaxStoredHashLength = 255;

    private const int IterationCount = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MinimumAcceptedIterationCount = 100_000;
    private const int MaximumAcceptedIterationCount = 1_000_000;
    private const string AlgorithmName = "pbkdf2-sha256";

    // Băm mật khẩu để lưu và sử dụng khi xác thực.
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationCount,
            HashAlgorithmName.SHA256,
            HashSize);
        var encodedHash = $"${AlgorithmName}${IterationCount.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        if (encodedHash.Length > MaxStoredHashLength)
        {
            throw new InvalidOperationException("The password hash exceeds the supported storage length.");
        }

        return encodedHash;
    }

    // Đối chiếu mật khẩu nhập vào với chuỗi băm đã lưu.
    public bool VerifyPassword(string? password, string? encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encodedHash) || encodedHash.Length > MaxStoredHashLength)
        {
            return false;
        }

        var parts = encodedHash.Split('$');
        if (parts.Length != 5 ||
            parts[0].Length != 0 ||
            !string.Equals(parts[1], AlgorithmName, StringComparison.Ordinal) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) ||
            iterations < MinimumAcceptedIterationCount ||
            iterations > MaximumAcceptedIterationCount)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expectedHash = Convert.FromBase64String(parts[4]);
            if (salt.Length != SaltSize || expectedHash.Length != HashSize)
            {
                return false;
            }

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
