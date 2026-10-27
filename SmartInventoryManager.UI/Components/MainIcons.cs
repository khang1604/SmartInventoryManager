namespace SmartInventoryManager.UI.Components;

internal static class MainIcons
{
    // Mỗi SVG trong Resources/Icons được nhúng theo tên file; supplier dùng icon shop.
    // Lấy biểu tượng SVG đã nhúng theo tên biểu tượng được yêu cầu.
    public static string Get(string name)
    {
        var resourceName = name == "supplier" ? "shop" : name;
        using var stream = typeof(MainIcons).Assembly.GetManifestResourceStream($"SmartInventoryManager.UI.Icon.{resourceName}")
            ?? throw new InvalidOperationException($"Missing UI icon: {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
