using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.Forms.Users;

public sealed class FrmUserDetail : Form
{
    public FrmUserDetail()
    {
        AutoScaleMode = AutoScaleMode.Font;
        var root = PreviewFormUi.Root(this, "Thông tin người dùng", new Size(680, 560));
        var body = ProductUi.Layout(1, 3);
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 248));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var fields = PreviewFormUi.Fields(3);
        PreviewFormUi.Field(fields, 0, 0, "txtUsername", "Tên đăng nhập *", 50);
        PreviewFormUi.Field(fields, 1, 0, "txtFullName", "Họ tên *", 100);
        PreviewFormUi.Field(fields, 0, 1, "txtEmail", "Email *", 100);
        PreviewFormUi.Field(fields, 1, 1, "cboRole", "Vai trò *", placeholder: "Chọn vai trò", unavailable: true);
        var password = PreviewFormUi.Field(fields, 0, 2, "txtPassword", "Mật khẩu *");
        password.UseSystemPasswordChar = true;
        var confirm = PreviewFormUi.Field(fields, 1, 2, "txtConfirmPassword", "Xác nhận mật khẩu *");
        confirm.UseSystemPasswordChar = true;
        body.Controls.Add(fields, 0, 0);
        body.Controls.Add(new CheckBox
        {
            Name = "chkIsActive", Text = "Đang hoạt động", Checked = true, Dock = DockStyle.Fill,
            Padding = new Padding(14, 0, 0, 0), TabIndex = 6,
            // [COLOR CONFIG]: vùng trạng thái mint với chữ tối.
            BackColor = AppColors.Mint, ForeColor = AppColors.Text
        }, 0, 1);
        root.Controls.Add(body, 0, 1);
    }
}
