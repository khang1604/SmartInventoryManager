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
        var title = ProductUi.Label($"{user?.FullName ?? "Chưa đăng nhập"}\n{role}", 11, true);
        title.Padding = new Padding(16, 0, 0, 0);
        var photo = ProductUi.Button("btnChangeAvatar", "Đổi ảnh");
        photo.Dock = DockStyle.Right; photo.Width = 120; photo.Enabled = false;
        photo.Visible = false;
        photo.AccessibleDescription = "Chức năng ảnh đại diện chưa được hỗ trợ";
        profile.Controls.Add(title); profile.Controls.Add(photo); profile.Controls.Add(avatar); body.Controls.Add(profile);
        var profileErrors = new AccountFieldErrors();
        var passwordErrors = new AccountFieldErrors();
        var fullName = new TextBox { Name = "txtFullName", AccessibleName = "Họ tên", Text = user?.FullName ?? "", MaxLength = 100 };
        var email = new TextBox { Name = "txtEmail", AccessibleName = "Email", Text = user?.Email ?? "", MaxLength = 100 };
        var saveProfile = ProductUi.Button("btnSaveAccount", "Lưu thông tin", primary: true);
        var profileNotice = Notice("Thông báo lưu thông tin");
        body.Controls.Add(SettingsUi.Group("Thông tin cá nhân", AppColors.Mint, 300,
            SettingsUi.Pair("Họ tên", profileErrors.Wrap(fullName)),
            SettingsUi.Pair("Email", profileErrors.Wrap(email)), Actions(saveProfile), profileNotice));
        body.Controls.Add(SettingsUi.Group("Thông tin công việc • Chỉ xem", AppColors.Lavender, 220,
            SettingsUi.Fields(("Tên đăng nhập", user?.Username ?? "", true), ("Quyền truy cập", user?.RoleName ?? "", true),
                ("Chức vụ", role, true), ("Kho làm việc", "Theo ngữ cảnh hiện tại", true))));

        var currentPassword = new TextBox { Name = "txtCurrentPassword", AccessibleName = "Mật khẩu hiện tại", UseSystemPasswordChar = true };
        var newPassword = new TextBox { Name = "txtNewPassword", AccessibleName = "Mật khẩu mới", UseSystemPasswordChar = true };
        var confirmPassword = new TextBox { Name = "txtConfirmPassword", AccessibleName = "Xác nhận mật khẩu mới", UseSystemPasswordChar = true };
        var toggle = ProductUi.Button("btnTogglePassword", "Đổi mật khẩu ▾", "key");
        toggle.Width = 200;
        var savePassword = ProductUi.Button("btnSavePassword", "Lưu mật khẩu", primary: true);
        var cancelPassword = ProductUi.Button("btnCancelPassword", "Hủy");
        var passwordNotice = Notice("Thông báo đổi mật khẩu");
        var passwordFields = ProductUi.Layout(1, 5);
        passwordFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 66, 66, 66, 48, 42 }) passwordFields.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        passwordFields.Controls.Add(SettingsUi.Pair("Mật khẩu hiện tại", passwordErrors.Wrap(currentPassword)), 0, 0);
        passwordFields.Controls.Add(SettingsUi.Pair("Mật khẩu mới · Ít nhất 8 ký tự", passwordErrors.Wrap(newPassword)), 0, 1);
        passwordFields.Controls.Add(SettingsUi.Pair("Xác nhận mật khẩu mới", passwordErrors.Wrap(confirmPassword)), 0, 2);
        passwordFields.Controls.Add(Actions(savePassword, cancelPassword), 0, 3);
        passwordFields.Controls.Add(passwordNotice, 0, 4);
        var security = SettingsUi.Group("Bảo mật", AppColors.Mint, 126, Actions(toggle));
        var securityRows = (TableLayoutPanel)security.Controls[0];
        securityRows.RowStyles[1] = new RowStyle(SizeType.Absolute, 48);
        securityRows.RowCount = 3;
        securityRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        securityRows.Controls.Add(passwordFields, 0, 2);
        passwordFields.Visible = false;
        body.Controls.Add(security);
        body.Controls.Add(new Label { AutoSize = true, Text = "Kho và vị trí không chỉnh sửa tại cài đặt tài khoản.", Padding = new Padding(8) });
        var close = ProductUi.Button("btnCancelAccount", "Đóng");
        close.DialogResult = DialogResult.Cancel; CancelButton = close; footer.Controls.Add(close);
        var available = services is not null && user is not null;
        bool busy = false;
        saveProfile.Enabled = savePassword.Enabled = toggle.Enabled = available;

        void SetBusy(bool value)
        {
            busy = value;
            saveProfile.Enabled = savePassword.Enabled = toggle.Enabled = available && !value;
            close.Enabled = cancelPassword.Enabled = !value;
            fullName.ReadOnly = email.ReadOnly = currentPassword.ReadOnly = newPassword.ReadOnly = confirmPassword.ReadOnly = value;
        }
        void ExpandPassword(bool expanded)
        {
            passwordFields.Visible = expanded;
            securityRows.RowStyles[2].Height = expanded ? 288 : 0;
            security.Height = expanded ? 414 : 126;
            toggle.Text = expanded ? "Đổi mật khẩu ▴" : "Đổi mật khẩu ▾";
            toggle.Width = 200;
            if (expanded) { body.ScrollControlIntoView(security); currentPassword.Focus(); }
            else
            {
                currentPassword.Clear(); newPassword.Clear(); confirmPassword.Clear();
                passwordErrors.Clear(); toggle.Focus();
            }
        }
        toggle.Click += (_, _) => { if (!busy) { passwordNotice.Text = ""; ExpandPassword(!passwordFields.Visible); } };
        cancelPassword.Click += (_, _) => { if (!busy) { passwordNotice.Text = ""; ExpandPassword(false); } };
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
        saveProfile.Click += async (_, _) =>
        {
            if (!available || busy) return;
            profileErrors.Clear(); profileNotice.Text = "";
            if (string.IsNullOrWhiteSpace(fullName.Text)) profileErrors.Set(fullName, "Nhập họ tên.");
            if (!string.IsNullOrWhiteSpace(email.Text) && !System.Net.Mail.MailAddress.TryCreate(email.Text.Trim(), out _)) profileErrors.Set(email, "Email không đúng định dạng.");
            if (profileErrors.FocusInvalid()) return;
            SetBusy(true);
            try
            {
                await services!.RunAsync(() => services.Users.UpdateAccountAsync(services.Session, new UpdateAccountRequest(fullName.Text, email.Text)));
                ShowNotice(profileNotice, "Đã lưu thông tin cá nhân.", false);
                title.Text = $"{fullName.Text.Trim()}\n{role}";
            }
            catch (Exception ex)
            {
                var message = AccountFieldErrors.SafeMessage(ex);
                if (message == "Email đã được sử dụng.") { profileErrors.Set(email, message); profileErrors.FocusInvalid(); }
                else ShowNotice(profileNotice, message, true);
            }
            finally { SetBusy(false); profileErrors.FocusInvalid(); }
        };
        savePassword.Click += async (_, _) =>
        {
            if (!available || busy) return;
            passwordErrors.Clear(); passwordNotice.Text = "";
            if (string.IsNullOrEmpty(currentPassword.Text)) passwordErrors.Set(currentPassword, "Nhập mật khẩu hiện tại.");
            if (newPassword.Text.Length < 8) passwordErrors.Set(newPassword, "Mật khẩu phải có ít nhất 8 ký tự.");
            if (string.IsNullOrEmpty(confirmPassword.Text) || confirmPassword.Text != newPassword.Text) passwordErrors.Set(confirmPassword, "Mật khẩu xác nhận không khớp.");
            if (passwordErrors.FocusInvalid()) return;
            SetBusy(true);
            try
            {
                await services!.RunAsync(() => services.Users.ChangePasswordAsync(services.Session,
                    new ChangePasswordRequest(currentPassword.Text, newPassword.Text, confirmPassword.Text)));
                ShowNotice(passwordNotice, "Đã đổi mật khẩu thành công.", false);
                ExpandPassword(false);
                // Kết quả vẫn hiện trong phần Bảo mật khi các ô mật khẩu đã thu lại.
                toggle.Text = "Đổi mật khẩu ▾ · Đã đổi thành công";
                toggle.Width = 340;
            }
            catch (Exception ex)
            {
                var message = AccountFieldErrors.SafeMessage(ex);
                if (message == "Mật khẩu hiện tại không đúng.") { passwordErrors.Set(currentPassword, message); passwordErrors.FocusInvalid(); }
                else ShowNotice(passwordNotice, message, true);
            }
            finally { SetBusy(false); passwordErrors.FocusInvalid(); }
        };
    }

    private static FlowLayoutPanel Actions(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty };
        panel.Controls.AddRange(controls);
        return panel;
    }
    private static Label Notice(string caption) => new() { Dock = DockStyle.Fill, AccessibleName = caption, Padding = new Padding(8), AutoSize = false };
    private static void ShowNotice(Label label, string message, bool error)
    {
        label.Text = message;
        // [COLOR CONFIG]: đỏ tối cho lỗi; chữ tối của theme cho xác nhận thành công.
        label.ForeColor = error ? Color.FromArgb(180, 32, 32) : AppColors.Text;
    }
}
