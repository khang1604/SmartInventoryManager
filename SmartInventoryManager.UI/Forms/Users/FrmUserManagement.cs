using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Users;

public partial class FrmUserManagement : Form
{
    public FrmUserManagement(UiServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        var page = new DataListPage<UserListItemDto>(
            services,
            "Người dùng",
            [("Username", "Tên đăng nhập"), ("FullName", "Họ tên"), ("Email", "Email"),
                ("RoleName", "Vai trò"), ("IsActive", "Trạng thái"), ("CreatedAt", "Ngày tạo")],
            async (_, search, _, _) => await services.Users.GetListAsync(
                services.Session,
                string.IsNullOrWhiteSpace(search) ? null : search.Trim()));

        page.Action("Thêm người dùng", async () =>
        {
            using var detail = new FrmUserDetail(services);
            if (detail.ShowDialog(this) == DialogResult.OK)
                await page.RefreshAsync();
        }, "plus-lg");
        Controls.Add(page);
    }
}
