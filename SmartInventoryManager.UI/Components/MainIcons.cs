using System.Resources;

namespace SmartInventoryManager.UI.Components;

internal static class MainIcons
{
    // SVG gốc từ Resources/Icons/bootstrap-icons-1.13.1, nhúng để không phụ thuộc thư mục chạy.
    private static readonly ResourceManager Resources = new(
        "SmartInventoryManager.UI.Properties.MainIcons", typeof(MainIcons).Assembly);

    public static string Get(string name)
    {
        if (name is "supplier" or "gear" or "person-gear" or "file-earmark-spreadsheet" or "file-earmark-pdf")
        {
            using var stream = typeof(MainIcons).Assembly.GetManifestResourceStream($"SmartInventoryManager.UI.Icon.{name}")
                ?? throw new InvalidOperationException($"Missing UI icon: {name}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        if (name == "person")
        {
            using var stream = typeof(MainIcons).Assembly.GetManifestResourceStream("SmartInventoryManager.UI.PersonIcon")
                ?? throw new InvalidOperationException("Missing person icon");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        return Resources.GetString(name) ?? throw new InvalidOperationException($"Missing UI icon: {name}");
    }
}
