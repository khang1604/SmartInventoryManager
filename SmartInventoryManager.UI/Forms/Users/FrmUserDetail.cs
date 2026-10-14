using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.Forms.Users;

public sealed class FrmUserDetail : Form
{
    public FrmUserDetail() : this(null) { }

    public FrmUserDetail(UiServices? services, UserListItemDto? user = null, bool resetPassword = false)
    {
        AutoScaleMode = AutoScaleMode.Font;
        var root = PreviewFormUi.Root(this, "Thông tin người dùng", new Size(760, 740));
        var body = ProductUi.Layout(1, 3);
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 434));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var fields = PreviewFormUi.Fields(4);
        var username = PreviewFormUi.Field(fields, 0, 0, "txtUsername", "Tên đăng nhập *", 50);
        var fullName = PreviewFormUi.Field(fields, 1, 0, "txtFullName", "Họ tên *", 100);
        var email = PreviewFormUi.Field(fields, 0, 1, "txtEmail", "Email *", 100, "vd: staff@smartinventory.local");
        fields.Controls.Add(ProductUi.Label("Vai trò *", 10, true), 1, 2);
        var role = LiveUi.Choice("cboRole", "Vai trò người dùng");
        role.Items.AddRange(["Chọn vai trò", "Quản trị viên", "Quản lý kho", "Nhân viên kho"]);
        role.SelectedIndex = 0; role.Dock = DockStyle.Fill;
        fields.Controls.Add(role, 1, 3);
        var contextNote = new Label { Text = services is null
                ? "Biểu mẫu chỉ xem trước khi chưa có phiên làm việc."
                : "Tài khoản được tạo theo vai trò đã chọn.",
            Dock = DockStyle.Top, AutoSize = true, MaximumSize = new Size(700, 0), Padding = new Padding(16, 8, 16, 0) };
        body.Controls.Add(contextNote, 0, 2);
        var password = PreviewFormUi.Field(fields, 0, 2, "txtPassword", "Mật khẩu *");
        password.UseSystemPasswordChar = true;
        var confirm = PreviewFormUi.Field(fields, 1, 2, "txtConfirmPassword", "Xác nhận mật khẩu *");
        confirm.UseSystemPasswordChar = true;
        var errors = new AccountFieldErrors();
        foreach (var input in new Control[] { username, fullName, email, role, password, confirm })
        {
            var position = fields.GetPositionFromControl(input);
            fields.Controls.Remove(input);
            fields.Controls.Add(errors.Wrap(input), position.Column, position.Row);
        }
        foreach (var row in new[] { 1, 3, 5 }) fields.RowStyles[row].Height = 76;
        contextNote.AutoSize = false; contextNote.Height = 70;
        // [COLOR CONFIG]: chữ đỏ cho lỗi lưu chung bên dưới biểu mẫu.
        var normalNoteColor = contextNote.ForeColor;
        if(services is not null)
        {
            foreach(var label in root.Controls.Cast<Control>().SelectMany(c=>c.Controls.Cast<Control>()).OfType<Label>().Where(l=>l.Text.Contains("Biểu mẫu xem trước"))) label.Text="Các trường có dấu * là bắt buộc";
            foreach(var label in root.Controls.Cast<Control>().SelectMany(c=>c.Controls.Cast<Control>()).OfType<Label>().Where(l=>l.Text=="Chưa kết nối dữ liệu")) label.Text="Tạo tài khoản mới";
        }
        body.Controls.Add(fields, 0, 0);
        var active = new CheckBox
        {
            Name = "chkIsActive", Text = "Đang hoạt động", Checked = true, Dock = DockStyle.Fill,
            Padding = new Padding(14, 0, 0, 0), TabIndex = 6,
            // [COLOR CONFIG]: vùng trạng thái mint với chữ tối.
            BackColor = AppColors.Mint, ForeColor = AppColors.Text
        };
        body.Controls.Add(active, 0, 1);
        if (user is not null)
        {
            username.Text = user.Username; username.ReadOnly = true;
            fullName.Text = user.FullName; email.Text = user.Email ?? string.Empty;
            role.SelectedIndex = user.RoleName switch { RoleNames.Admin => 1, RoleNames.WarehouseManager => 2, RoleNames.Staff => 3, _ => 0 };
            active.Checked = user.IsActive;
            fullName.Enabled = email.Enabled = role.Enabled = active.Enabled = !resetPassword;
            password.Enabled = confirm.Enabled = resetPassword;
            contextNote.Text = resetPassword ? "Nhập và xác nhận mật khẩu mới cho tài khoản đã chọn." : "Sửa thông tin tài khoản; thao tác này giữ nguyên mật khẩu.";
            Text = resetPassword ? "Đặt lại mật khẩu" : "Sửa thông tin người dùng";
        }
        root.Controls.Add(body, 0, 1);

        if (services is not null)
        {
            var status = FindControl<Label>(root, "formStatus");
            status.Text = "Đã kết nối dữ liệu";
            FindControl<Label>(root, "formNote").Text = "Các trường có dấu * là bắt buộc • Nhập email đúng định dạng để lưu tài khoản.";
            var save = FindControl<AntdUI.Button>(root, "btnSave");
            save.Enabled = true;
            save.AccessibleDescription = "Lưu tài khoản nhân viên";
            save.Click += async (_, _) =>
            {
                if (!save.Enabled) return;
                errors.Clear(); contextNote.ForeColor = normalNoteColor;
                if (string.IsNullOrWhiteSpace(username.Text)) errors.Set(username, "Nhập tên đăng nhập.");
                if (string.IsNullOrWhiteSpace(fullName.Text)) errors.Set(fullName, "Nhập họ tên.");
                if (!resetPassword && !string.IsNullOrWhiteSpace(email.Text) && !System.Net.Mail.MailAddress.TryCreate(email.Text.Trim(), out _)) errors.Set(email, "Email không đúng định dạng.");
                if (role.SelectedIndex < 1) errors.Set(role, "Chọn vai trò.");
                if (user is null || resetPassword)
                {
                    if (password.Text.Length < 8) errors.Set(password, "Mật khẩu phải có ít nhất 8 ký tự.");
                    if (confirm.Text != password.Text || string.IsNullOrEmpty(confirm.Text)) errors.Set(confirm, "Mật khẩu xác nhận không khớp.");
                }
                if (errors.FocusInvalid()) return;
                save.Enabled = false;
                try
                {
                    var roleName = role.SelectedIndex switch
                    {
                        1 => RoleNames.Admin,
                        2 => RoleNames.WarehouseManager,
                        3 => RoleNames.Staff,
                        _ => string.Empty
                    };
                    if (user is null)
                        await services.RunAsync(() => services.Users.CreateAsync(services.Session,
                            new CreateUserRequest(username.Text, fullName.Text, email.Text, password.Text,
                                confirm.Text, roleName, active.Checked)));
                    else if (resetPassword)
                        await services.RunAsync(() => services.Users.ResetPasswordAsync(services.Session, user.UserId,
                            new ResetUserPasswordRequest(password.Text, confirm.Text)));
                    else
                        await services.RunAsync(() => services.Users.UpdateUserAsync(services.Session, user.UserId,
                            new UpdateUserRequest(fullName.Text, email.Text, roleName, active.Checked)));
                    MessageBox.Show(this, user is null ? "Đã tạo tài khoản thành công." : resetPassword ? "Đã đặt lại mật khẩu." : "Đã cập nhật tài khoản.", "Thành công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    save.Enabled = true;
                    errors.Clear();
                    var message = AccountFieldErrors.SafeMessage(ex);
                    if (message == "Tên đăng nhập đã tồn tại.") errors.Set(username, message);
                    else if (message == "Email đã được sử dụng.") errors.Set(email, message);
                    else if (message == "Vai trò không tồn tại trong hệ thống.") errors.Set(role, message);
                    else { contextNote.Text = message; contextNote.ForeColor = Color.FromArgb(180, 32, 32); } // [COLOR CONFIG]: lỗi lưu chung màu đỏ.
                    errors.FocusInvalid();
                }
            };
        }
    }

    private static T FindControl<T>(Control root, string name) where T : Control
    {
        foreach (Control control in root.Controls)
        {
            if (control.Name == name && control is T match) return match;
            if (control.HasChildren)
            {
                try { return FindControl<T>(control, name); }
                catch (InvalidOperationException) { }
            }
        }
        throw new InvalidOperationException($"Không tìm thấy control {name}.");
    }
}
