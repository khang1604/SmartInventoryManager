using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal static class ProductUi
{
    public static TableLayoutPanel Layout(int columns, int rows) => new()
    {
        Dock = DockStyle.Fill, ColumnCount = columns, RowCount = rows,
        Margin = Padding.Empty, GrowStyle = TableLayoutPanelGrowStyle.FixedSize
    };

    public static Label Label(string text, float size = 10, bool bold = false) => new()
    {
        Text = text, Dock = DockStyle.Fill, AutoEllipsis = true, Margin = Padding.Empty,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        TextAlign = ContentAlignment.MiddleLeft,
        // [COLOR CONFIG]: chữ chính theo bảng màu đã duyệt.
        ForeColor = AppColors.Text
    };

    public static AntdUI.Input Input(string name, string placeholder = "") => new()
    {
        Name = name, PlaceholderText = placeholder, Dock = DockStyle.Fill,
        Radius = 6, Margin = new Padding(0, 0, 12, 0), AccessibleName = name,
        // [COLOR CONFIG]: nền trắng của ô nhập.
        BackColor = AppColors.Surface,
        // [COLOR CONFIG]: viền nhẹ, viền focus tím.
        BorderColor = AppColors.Border, BorderActive = AppColors.Primary,
        // [COLOR CONFIG]: viền hover lavender.
        BorderHover = AppColors.Lavender
    };

    public static AntdUI.Button Button(string name, string text, string? icon = null, bool primary = false)
    {
        return new AntdUI.Button
        {
            Name = name, Text = text, AccessibleName = text, Radius = 6, WaveSize = 0,
            Size = new Size(150, 44), Margin = new Padding(0, 0, 10, 0),
            Font = new Font("Segoe UI", 10F), Cursor = Cursors.Hand, UseMnemonic = false,
            IconSvg = icon is null ? null : MainIcons.Get(icon), IconSize = new Size(22, 22),
            IconGap = 0.6F, TextCenterHasIcon = true, BorderWidth = primary ? 0 : 1,
            Type = primary ? AntdUI.TTypeMini.Primary : AntdUI.TTypeMini.Default,
            // [COLOR CONFIG]: nút chính tím, nút phụ trắng.
            DefaultBack = primary ? AppColors.Primary : AppColors.Surface,
            // [COLOR CONFIG]: màu nền ghi đè màu xanh mặc định của AntdUI.
            BackColor = primary ? AppColors.Primary : AppColors.Surface,
            // [COLOR CONFIG]: viền nhạt của nút phụ.
            DefaultBorderColor = AppColors.Border,
            // [COLOR CONFIG]: chữ nút chính trắng, chữ nút phụ tối.
            ForeColor = primary ? SystemColors.HighlightText : AppColors.Text,
            // [COLOR CONFIG]: tương tác dùng lavender và primary.
            BackHover = AppColors.Lavender, BackActive = AppColors.Primary
        };
    }
}
