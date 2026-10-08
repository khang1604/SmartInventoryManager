using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Components;

internal sealed class AccountPopover : ToolStripDropDown
{
    public AccountPopover(string role, int width, Action appSettings, Action accountSettings, Action logout)
    {
        AutoSize = false; Padding = Padding.Empty; Margin = Padding.Empty;
        Size = new Size(width, 330); AutoClose = true;
        // [COLOR CONFIG]: nền trắng cho thanh tài khoản mở lên.
        BackColor = AppColors.Surface;
        var panel = new RoundedSurface { Size = Size, Padding = new Padding(10) };
        var root = ProductUi.Layout(1, 5);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        var header = ProductUi.Layout(2, 1);
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var avatar = SettingsUi.Avatar(40); avatar.Margin = new Padding(0, 18, 8, 0);
        header.Controls.Add(avatar, 0, 0);
        var heading = ProductUi.Label($"Tài khoản của tôi\n{role}", 9, true);
        header.Controls.Add(heading, 1, 0); root.Controls.Add(header, 0, 0);
        header.Paint += (_, e) =>
        {
            // [COLOR CONFIG]: đường phân chia nhẹ dưới thông tin tài khoản.
            using var pen = new Pen(AppColors.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        var spacer = new Panel { Dock = DockStyle.Fill };
        spacer.Paint += (_, e) =>
        {
            // [COLOR CONFIG]: phân cách Đăng xuất với hai mục cài đặt.
            using var pen = new Pen(AppColors.Border);
            e.Graphics.DrawLine(pen, 0, spacer.Height - 1, spacer.Width, spacer.Height - 1);
        };
        root.Controls.Add(spacer, 0, 3);
        void AddAction(string name, string caption, string icon, int row, Action action)
        {
            var button = ProductUi.Button(name, caption, icon);
            button.Dock = DockStyle.Fill; button.Margin = new Padding(0, 4, 0, 4); button.BorderWidth = 0;
            button.Font = new Font("Segoe UI", 9);
            if (row == 4)
            {
                // [COLOR CONFIG]: chữ đỏ đậm cho Đăng xuất, vẫn có nhãn và icon rõ ràng.
                button.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Color.FromArgb(170, 35, 50);
            }
            button.Click += (_, _) => { Close(); action(); };
            root.Controls.Add(button, 0, row);
        }
        AddAction("btnAppSettings", "Cài đặt ứng dụng", "gear", 1, appSettings);
        AddAction("btnAccountSettings", "Cài đặt tài khoản", "person-gear", 2, accountSettings);
        AddAction("btnAccountLogout", "Đăng xuất", "box-arrow-right", 4, logout);
        panel.Controls.Add(root);
        var host = new ToolStripControlHost(panel) { AutoSize = false, Size = Size, Margin = Padding.Empty, Padding = Padding.Empty };
        Items.Add(host);
        using var outline = new System.Drawing.Drawing2D.GraphicsPath();
        outline.AddRoundedRectangle(new Rectangle(Point.Empty, Size), new Size(12, 12));
        Region = new Region(outline);
    }
}
