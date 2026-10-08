using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Users;

public partial class FrmUserManagement : Form
{
    public FrmUserManagement() : this(null) { }

    public FrmUserManagement(UiServices? services)
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmUserManagement",
            [("Tìm kiếm", "Nhập tài khoản hoặc họ tên"), ("Vai trò", "Tất cả vai trò"), ("Trạng thái", "Tất cả trạng thái")],
            ["Tên đăng nhập", "Họ tên", "Email", "Vai trò", "Trạng thái", "Ngày tạo"], "people", "Thêm người dùng", () => new FrmUserDetail(services), ["Sửa thông tin", "Ngừng hoạt động"]));
    }
}
