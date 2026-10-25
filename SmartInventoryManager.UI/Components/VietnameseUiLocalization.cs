namespace SmartInventoryManager.UI.Components;

internal static class VietnameseUiLocalization
{
    public static void Apply()
    {
        if (AntdUI.Localization.Provider is not VietnameseProvider)
            AntdUI.Localization.Provider = new VietnameseProvider(AntdUI.Localization.Provider);
        if (AntdUI.Localization.CurrentLanguage != "vi-VN")
            AntdUI.Localization.SetLanguage("vi-VN");
    }

    // Giữ bộ dịch hiện có và fallback của thư viện cho các khóa khác.
    private sealed class VietnameseProvider(AntdUI.ILocalization? previous) : AntdUI.ILocalization
    {
        public string GetLocalizedString(string key) => key == "NoData"
            ? "Không có dữ liệu phù hợp."
            : previous?.GetLocalizedString(key)!;
    }
}
