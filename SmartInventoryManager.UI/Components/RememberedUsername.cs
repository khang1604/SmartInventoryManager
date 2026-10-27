using System.Diagnostics;
using System.Text.Json;

namespace SmartInventoryManager.UI.Components;

// Local convenience only: this file never contains passwords, tokens or an authenticated session.
internal sealed record RememberedUsername(string? Username)
{
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VioWare", "remembered-username.json");

    // Đọc tên đăng nhập đã được người dùng chọn ghi nhớ trên máy.
    internal static string? Load(string? path = null)
    {
        try
        {
            var file = path ?? FilePath;
            if (!File.Exists(file)) return null;
            var value = JsonSerializer.Deserialize<RememberedUsername>(File.ReadAllText(file))?.Username;
            return string.IsNullOrWhiteSpace(value) || value.Length > 50 ? null : value;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Debug.WriteLine(ex); return null;
        }
    }

    // Lưu lựa chọn ghi nhớ tên đăng nhập và bỏ qua lỗi lưu cục bộ.
    internal static bool TrySave(string? username, string? path = null)
    {
        try
        {
            var file = path ?? FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
            var temporary = file + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new RememberedUsername(username)));
            File.Move(temporary, file, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine(ex); return false;
        }
    }
}
