using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Forms.Settings;

internal sealed class FrmAccountSettings : Form
{
    public FrmAccountSettings(AuthenticatedUserDto? user, string role)
    {
        var (_, body, footer) = SettingsUi.Root(this, "Cài đặt tài khoản", "Thông tin nhân viên • Chức năng lưu hồ sơ đang được bổ sung", new Size(820, 780));
        var profile = new RoundedSurface { Height = 88, Margin = new Padding(0, 0, 0, 12) };
        var avatar = SettingsUi.Avatar(); avatar.Dock = DockStyle.Left;
        var title = ProductUi.Label($"{user?.FullName ?? "Chưa đăng nhập"}\n{role}", 11, true); title.Padding = new Padding(16, 0, 0, 0);
        var photo = ProductUi.Button("btnChangeAvatar", "Đổi ảnh"); photo.Dock = DockStyle.Right; photo.Width = 120; photo.Enabled = false;
        photo.AccessibleDescription = "Chức năng lưu avatar chưa được hỗ trợ";
        profile.Controls.Add(title); profile.Controls.Add(photo); profile.Controls.Add(avatar); body.Controls.Add(profile);
        body.Controls.Add(SettingsUi.Group("Thông tin cá nhân", AppColors.Mint, 212,
            SettingsUi.Fields(("Họ tên", user?.FullName ?? "", false), ("Email", user?.Email ?? "", false),
                ("Số điện thoại", "Chưa có thông tin", true), ("Địa chỉ liên hệ", "Chưa có thông tin", true))));
        body.Controls.Add(SettingsUi.Group("Thông tin công việc • Chỉ xem", AppColors.Lavender, 280,
            SettingsUi.Fields(("Mã nhân viên", "Chưa có thông tin", true), ("Tên đăng nhập", user?.Username ?? "", true),
                ("Quyền truy cập", user?.RoleName ?? "", true), ("Chức vụ", role, true),
                ("Kho làm việc", "Chưa có thông tin phân công", true), ("Vị trí", "Chưa có thông tin phân công", true))));
        var password = ProductUi.Button("btnChangePassword", "Đổi mật khẩu"); password.Enabled = false;
        body.Controls.Add(SettingsUi.Group("Bảo mật", AppColors.Mint, 112, SettingsUi.Pair("Mật khẩu tài khoản", password)));
        var notice = new Label { AutoSize = false, Height = 64, Text = "Chức năng lưu hồ sơ, avatar và đổi mật khẩu đang được bổ sung. Liên hệ quản trị viên để cập nhật thông tin công việc.", Padding = new Padding(8), AutoEllipsis = true };
        body.Controls.Add(notice);
        var save = ProductUi.Button("btnSaveAccount", "Lưu thay đổi", primary: true); save.Enabled = false;
        var cancel = ProductUi.Button("btnCancelAccount", "Đóng"); cancel.Width = 100; cancel.DialogResult = DialogResult.Cancel; CancelButton = cancel;
        footer.Controls.AddRange([save, cancel]);
    }
}
