using System.Resources;

namespace SmartInventoryManager.UI.Components;

internal static class MainIcons
{
    // SVG gốc từ Resources/Icons/bootstrap-icons-1.13.1, nhúng để không phụ thuộc thư mục chạy.
    private static readonly ResourceManager Resources = new(
        "SmartInventoryManager.UI.Properties.MainIcons", typeof(MainIcons).Assembly);

    public static string Get(string name) => Resources.GetString(name)
        ?? throw new InvalidOperationException($"Missing UI icon: {name}");
}
