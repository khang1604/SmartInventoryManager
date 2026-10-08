using System.Diagnostics;
using System.Text.Json;

namespace SmartInventoryManager.UI.Components;

internal sealed record UiPreferences(float FontSize = 10, bool CollapsedMenu = false, int PageSize = 50)
{
    public static UiPreferences Current { get; private set; } = Load();
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VioWare", "ui-settings.json");
    private static bool IsValid(UiPreferences value) => value.FontSize is 9 or 10 or 11 or 12 && value.PageSize is 25 or 50 or 100;
    private static UiPreferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var value = JsonSerializer.Deserialize<UiPreferences>(File.ReadAllText(FilePath));
                if (value is not null && IsValid(value)) return value;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { Debug.WriteLine(ex); }
        return new();
    }
    public static void Save(UiPreferences value)
    {
        if (!IsValid(value)) throw new ArgumentException("Cài đặt hiển thị không hợp lệ.");
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value));
        File.Move(temporary, FilePath, overwrite: true);
        Current = value;
    }
}
