using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;
using SmartInventoryManager.UI.Themes;

namespace SmartInventoryManager.Forms.Users;

public sealed class FrmUserDetail : Form
{
    public FrmUserDetail() : this(null) { }

    public FrmUserDetail(UiServices? services)
    {
        AutoScaleMode = AutoScaleMode.Font;
        var root = PreviewFormUi.Root(this, "Thông tin người dùng", new Size(760, 660));
        var body = ProductUi.Layout(1, 3);
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 338));
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
        fields.Controls.Add(ProductUi.Label("Kho phụ trách", 10, true), 0, 6);
        fields.Controls.Add(ProductUi.Label("Vị trí trong kho", 10, true), 1, 6);
        var warehouse = LiveUi.Choice("cboUserWarehouse", "Kho phụ trách");
        warehouse.Dock = DockStyle.Fill; warehouse.Enabled = false;
        warehouse.Items.Add("Chưa thuộc chức năng tạo tài khoản"); warehouse.SelectedIndex = 0;
        var location = LiveUi.Choice("cboUserLocation", "Vị trí trong kho đã chọn");
        location.Dock = DockStyle.Fill; location.Enabled = false;
        location.Items.Add("Chưa thuộc chức năng tạo tài khoản"); location.SelectedIndex = 0;
        fields.Controls.Add(warehouse, 0, 7); fields.Controls.Add(location, 1, 7);
        var contextNote = new Label { Text = services is null
                ? "Biểu mẫu chỉ xem trước khi chưa có phiên làm việc."
                : "Tài khoản được tạo theo vai trò đã chọn.",
            Dock = DockStyle.Top, AutoSize = true, MaximumSize = new Size(700, 0), Padding = new Padding(16, 8, 16, 0) };
        body.Controls.Add(contextNote, 0, 2);
        var password = PreviewFormUi.Field(fields, 0, 2, "txtPassword", "Mật khẩu *");
        password.UseSystemPasswordChar = true;
        var confirm = PreviewFormUi.Field(fields, 1, 2, "txtConfirmPassword", "Xác nhận mật khẩu *");
        confirm.UseSystemPasswordChar = true;
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
                if (string.IsNullOrWhiteSpace(username.Text) || string.IsNullOrWhiteSpace(fullName.Text) || role.SelectedIndex<1 || !System.Net.Mail.MailAddress.TryCreate(email.Text,out _) || string.IsNullOrWhiteSpace(password.Text) || password.Text!=confirm.Text)
                { MessageBox.Show(this,"Nhập đủ tên đăng nhập, họ tên, email hợp lệ, vai trò và mật khẩu xác nhận khớp nhau.","Kiểm tra thông tin",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; }
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
                    await services.RunAsync(() => services.Users.CreateAsync(services.Session,
                        new CreateUserRequest(username.Text, fullName.Text, email.Text, password.Text,
                            confirm.Text, roleName, active.Checked)));
                    MessageBox.Show(this, "Đã tạo tài khoản thành công.", "Thành công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    save.Enabled = true;
                    MessageBox.Show(this, LiveUi.Error(ex), "Không thể lưu",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
