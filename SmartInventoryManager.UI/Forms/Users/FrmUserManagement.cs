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
        if(services is not null) {
            var role=LiveUi.Choice("cboUserRole","Vai trò"); role.Items.AddRange(["Tất cả vai trò","Quản trị viên","Quản lý kho","Nhân viên kho"]); role.SelectedIndex=0;
            var status=LiveUi.Choice("cboUserStatus","Trạng thái"); status.Items.AddRange(["Tất cả trạng thái","Đang hoạt động","Ngừng hoạt động"]); status.SelectedIndex=0;
            var page=new DataListPage<UserListItemDto>(services,"Người dùng",[("Username","Tên đăng nhập"),("FullName","Họ tên"),("Email","Email"),("RoleName","Vai trò"),("IsActive","Trạng thái"),("CreatedAt","Ngày tạo")],
                (_,search,_,_)=>services.Users.GetListAsync(services.Session,search,role.SelectedIndex switch {1=>"ADMIN",2=>"WAREHOUSE_MANAGER",3=>"STAFF",_=>null},status.SelectedIndex switch {1=>true,2=>false,_=>(bool?)null}));
            page.AddFilter("Vai trò",role); page.AddFilter("Trạng thái",status); page.AddDetailsColumn();
            page.Action("Thêm người dùng",async()=> { using var detail=new FrmUserDetail(services); if(detail.ShowDialog(this)==DialogResult.OK) await page.RefreshAsync(); },"plus-lg");
            if (services.Authorization.CanManageUser(services.Session))
            {
                page.SelectedAction("Xóa tài khoản", async (user, _) =>
                {
                    var confirm = MessageBox.Show(this,
                        $"Vô hiệu hóa tài khoản '{user.Username}'? Dữ liệu lịch sử của tài khoản sẽ được giữ lại.",
                        "Xác nhận vô hiệu hóa tài khoản",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;

                    await services.RunAsync(() => services.Users.DeactivateAsync(services.Session, user.UserId));
                }, "trash", (user, _) => Task.FromResult(
                    user.IsActive && user.UserId != services.Session.CurrentUser?.UserId));
            }
            role.SelectedIndexChanged+=async(_,_)=>await page.RefreshAsync(); status.SelectedIndexChanged+=async(_,_)=>await page.RefreshAsync();
            Controls.Add(page); return;
        }
        Controls.Add(WorkflowUi.List("FrmUserManagement",
            [("Tìm kiếm", "Nhập tài khoản hoặc họ tên"), ("Vai trò", "Tất cả vai trò"), ("Trạng thái", "Tất cả trạng thái")],
            ["Tên đăng nhập", "Họ tên", "Email", "Vai trò", "Trạng thái", "Ngày tạo"], "people", "Thêm người dùng", () => new FrmUserDetail(services), ["Sửa thông tin", "Ngừng hoạt động"]));
    }
}
