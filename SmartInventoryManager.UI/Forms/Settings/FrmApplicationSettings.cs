using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Forms.Settings;

internal sealed class FrmApplicationSettings : Form
{
    public FrmApplicationSettings()
    {
        var (_, body, footer) = SettingsUi.Root(this, "Cài đặt ứng dụng", "Tùy chỉnh cách hiển thị trên máy này", new Size(690, 690));
        var font = LiveUi.Choice("cboFontSize", "Cỡ chữ"); font.Items.AddRange(["Nhỏ", "Tiêu chuẩn", "Lớn", "Rất lớn"]);
        font.SelectedIndex = (int)UiPreferences.Current.FontSize - 9;
        var menu = new FlowLayoutPanel { Height = 36, WrapContents = false, Margin = Padding.Empty };
        RadioButton MenuOption(string caption) => new()
        {
            Text = caption, Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleCenter, Size = new Size(130, 34), Margin = Padding.Empty,
            AccessibleName = "Menu mặc định: " + caption,
            // [COLOR CONFIG]: lựa chọn menu dùng lavender, viền nhạt và chữ tối.
            BackColor = AppColors.Surface, ForeColor = AppColors.Text,
            FlatAppearance = { BorderColor = AppColors.Border, CheckedBackColor = AppColors.Lavender, MouseOverBackColor = AppColors.Sky }
        };
        var expanded = MenuOption("Mở rộng"); var compact = MenuOption("Thu gọn");
        menu.Controls.AddRange([expanded, compact]);
        compact.Checked = UiPreferences.Current.CollapsedMenu; expanded.Checked = !compact.Checked;
        var page = LiveUi.Choice("cboPageSize", "Số dòng mỗi trang"); page.Items.AddRange(["25 dòng", "50 dòng", "100 dòng"]);
        page.SelectedIndex = UiPreferences.Current.PageSize switch { 25 => 0, 100 => 2, _ => 1 };
        body.Controls.Add(SettingsUi.Group("Giao diện", AppColors.Sky, 160, SettingsUi.Pair("Cỡ chữ", font), SettingsUi.Pair("Menu mặc định", menu)));
        body.Controls.Add(SettingsUi.Group("Hiển thị bảng", AppColors.Mint, 138, SettingsUi.Pair("Số dòng mỗi trang", page),
            ProductUi.Label("Áp dụng cho bảng sản phẩm có phân trang; các bảng khác cuộn để xem thêm.", 9)));
        body.Controls.Add(SettingsUi.Group("Thông tin ứng dụng", AppColors.Lavender, 150,
            SettingsUi.Pair("Ứng dụng", ProductUi.Label(UiBrand.ApplicationName)), SettingsUi.Pair("Dự án", ProductUi.Label(UiBrand.ProjectName))));
        var save = ProductUi.Button("btnSaveAppSettings", "Lưu cài đặt", primary: true);
        var cancel = ProductUi.Button("btnCancelSettings", "Hủy"); cancel.Width = 90; cancel.DialogResult = DialogResult.Cancel; CancelButton = cancel;
        var reset = ProductUi.Button("btnResetSettings", "Khôi phục mặc định"); reset.Width = 200;
        reset.Click += (_, _) => { font.SelectedIndex = 1; expanded.Checked = true; page.SelectedIndex = 1; };
        save.Click += (_, _) =>
        {
            try
            {
                UiPreferences.Save(new UiPreferences(9 + font.SelectedIndex, compact.Checked, page.SelectedIndex switch { 0 => 25, 2 => 100, _ => 50 }));
                DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                MessageBox.Show(this, "Không lưu được cài đặt trên máy này. Vui lòng thử lại.", "Cài đặt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        footer.Controls.AddRange([save, cancel, reset]);
    }
}
