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
        PreviewFormUi.Field(fields, 0, 0, "txtUsername", "Tên đăng nhập *", 50);
        PreviewFormUi.Field(fields, 1, 0, "txtFullName", "Họ tên *", 100);
        PreviewFormUi.Field(fields, 0, 1, "txtEmail", "Email *", 100);
        fields.Controls.Add(ProductUi.Label("Vai trò *", 10, true), 1, 2);
        var role = LiveUi.Choice("cboRole", "Vai trò người dùng");
        role.Items.AddRange(["Chọn vai trò", "Quản trị viên", "Quản lý kho", "Nhân viên kho"]);
        role.SelectedIndex = 0; role.Dock = DockStyle.Fill;
        fields.Controls.Add(role, 1, 3);
        fields.Controls.Add(ProductUi.Label("Kho phụ trách", 10, true), 0, 6);
        fields.Controls.Add(ProductUi.Label("Vị trí trong kho", 10, true), 1, 6);
        var warehouse = LiveUi.Choice("cboUserWarehouse", "Kho phụ trách");
        warehouse.Dock = DockStyle.Fill; warehouse.Enabled = false;
        warehouse.Items.Add("Chưa có danh sách kho"); warehouse.SelectedIndex = 0;
        var location = LiveUi.Choice("cboUserLocation", "Vị trí trong kho đã chọn");
        location.Dock = DockStyle.Fill; location.Enabled = false;
        location.Items.Add("Chọn kho trước"); location.SelectedIndex = 0;
        fields.Controls.Add(warehouse, 0, 7); fields.Controls.Add(location, 1, 7);
        if (services is not null)
        {
            var contextNote = new Label { Text = "Biểu mẫu chưa lưu được tài khoản. Vị trí sẽ được bổ sung khi có danh sách từ hệ thống.",
                Dock = DockStyle.Top, AutoSize = true, MaximumSize = new Size(700, 0), Padding = new Padding(16, 8, 16, 0) };
            Load += async (_, _) =>
            {
                try
                {
                    var warehouses = await services.RunAsync(() => services.Warehouses.GetActiveAsync(services.Session));
                    if (IsDisposed) return;
                    warehouse.Items.Clear();
                    warehouse.DisplayMember = "Name";
                    warehouse.ValueMember = "Id";
                    warehouse.DataSource = warehouses.ToList();
                    warehouse.Enabled = warehouses.Count > 0;
                    if (warehouses.Count == 0) contextNote.Text = "Chưa có kho đang hoạt động. Biểu mẫu chưa lưu được tài khoản.";
                }
                catch (Exception ex) { if (!IsDisposed) contextNote.Text = LiveUi.LoadError(ex); }
            };
            body.Controls.Add(contextNote, 0, 2);
        }
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
