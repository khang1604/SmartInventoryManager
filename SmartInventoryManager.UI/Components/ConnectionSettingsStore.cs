using System.Security.Cryptography;
using System.Text;

namespace SmartInventoryManager.UI.Components;

internal sealed class ConnectionSettingsStore(string? filePath = null)
{
    private readonly string _filePath = filePath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VioWare", "connection.dat");

    public string? Load(out bool unreadable)
    {
        unreadable = false;
        if (!File.Exists(_filePath)) return null;
        try
        {
            if (new FileInfo(_filePath).Length > 65536) throw new CryptographicException();
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(_filePath), null, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(plain); }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            unreadable = true;
            return null;
        }
    }

    public void Save(string connectionString)
    {
        var plain = Encoding.UTF8.GetBytes(connectionString);
        byte[] encrypted;
        try { encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser); }
        finally { CryptographicOperations.ZeroMemory(plain); }
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporary = _filePath + ".tmp";
        File.WriteAllBytes(temporary, encrypted);
        File.Move(temporary, _filePath, overwrite: true);
    }
}
