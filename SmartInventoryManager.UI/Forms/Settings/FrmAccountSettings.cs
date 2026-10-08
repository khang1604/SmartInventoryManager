using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.UI.Forms.Settings;

internal sealed class FrmAccountSettings : Form
{
    public FrmAccountSettings(AuthenticatedUserDto? user, string role, UiServices? services = null)
    {
        var (_, body, footer) = SettingsUi.Root(this, "Cài đặt tài khoản", "Thông tin nhân viên", new Size(820, 780));
        var profile = new RoundedSurface { Height = 88, Margin = new Padding(0, 0, 0, 12) };
        var avatar = SettingsUi.Avatar(); avatar.Dock = DockStyle.Left;
        var title = ProductUi.Label($"{user?.FullName ?? "Chưa đăng nhập"}\n{role}", 11, true); title.Padding = new Padding(16, 0, 0, 0);
        var photo = ProductUi.Button("btnChangeAvatar", "Đổi ảnh"); photo.Dock = DockStyle.Right; photo.Width = 120; photo.Enabled = false;
        photo.AccessibleDescription = "Chức năng ảnh đại diện chưa được hỗ trợ";
        profile.Controls.Add(title); profile.Controls.Add(photo); profile.Controls.Add(avatar); body.Controls.Add(profile);
        var fullName = new TextBox { Text = user?.FullName ?? "", MaxLength = 100, Dock = DockStyle.Fill };
        var email = new TextBox { Text = user?.Email ?? "", MaxLength = 100, Dock = DockStyle.Fill };
        body.Controls.Add(SettingsUi.Group("Thông tin cá nhân", AppColors.Mint, 150,
            SettingsUi.Pair("Họ tên", fullName), SettingsUi.Pair("Email", email)));
        body.Controls.Add(SettingsUi.Group("Thông tin công việc • Chỉ xem", AppColors.Lavender, 220,
            SettingsUi.Fields(("Tên đăng nhập", user?.Username ?? "", true), ("Quyền truy cập", user?.RoleName ?? "", true),
                ("Chức vụ", role, true), ("Kho làm việc", "Theo ngữ cảnh hiện tại", true))));
        var currentPassword = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        var newPassword = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        var confirmPassword = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Fill };
        body.Controls.Add(SettingsUi.Group("Bảo mật", AppColors.Mint, 190,
            SettingsUi.Pair("Mật khẩu hiện tại", currentPassword), SettingsUi.Pair("Mật khẩu mới", newPassword),
            SettingsUi.Pair("Xác nhận mật khẩu mới", confirmPassword)));
        var notice = new Label { AutoSize = false, Height = 44, Text = "Kho và vị trí không chỉnh sửa tại cài đặt tài khoản.", Padding = new Padding(8), AutoEllipsis = true };
        body.Controls.Add(notice);
        var save = ProductUi.Button("btnSaveAccount", "Lưu thay đổi", primary: true);
        var cancel = ProductUi.Button("btnCancelAccount", "Đóng"); cancel.Width = 100; cancel.DialogResult = DialogResult.Cancel; CancelButton = cancel;
        save.Enabled = services is not null && user is not null;
        save.Click += async (_, _) =>
        {
            if (services is null || user is null) return;
            save.Enabled = false;
            try
            {
                await services.RunAsync(() => services.Users.UpdateAccountAsync(services.Session,
                    new UpdateAccountRequest(fullName.Text, email.Text)));
                if (!string.IsNullOrEmpty(currentPassword.Text) || !string.IsNullOrEmpty(newPassword.Text) || !string.IsNullOrEmpty(confirmPassword.Text))
                {
                    await services.RunAsync(() => services.Users.ChangePasswordAsync(services.Session,
                        new ChangePasswordRequest(currentPassword.Text, newPassword.Text, confirmPassword.Text)));
                }
                MessageBox.Show(this, "Đã lưu cài đặt tài khoản.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                save.Enabled = true;
                MessageBox.Show(this, LiveUi.Error(ex), "Không thể lưu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        footer.Controls.AddRange([save, cancel]);
    }
}
