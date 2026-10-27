using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Inventory;

public partial class FrmInventory : Form
{
    // Khởi tạo màn hình theo dõi tồn và thao tác điều chỉnh, chuyển kho.
    public FrmInventory(UiServices services)
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(ConnectedPages.Inventory(services));
    }
    // Khởi tạo màn hình theo dõi tồn và thao tác điều chỉnh, chuyển kho.
    public FrmInventory()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmInventory",
            [("Tìm kiếm", "Nhập mã hoặc tên sản phẩm"), ("Kho", "Tất cả kho"), ("Trạng thái tồn", "Tất cả trạng thái")],
            ["Mã sản phẩm", "Tên sản phẩm", "Kho", "Đơn vị", "Số lượng tồn", "Điểm đặt hàng lại", "Tồn tối đa", "Trạng thái tồn", "Cập nhật"], "boxes", pendingActions: ["Điều chỉnh tồn", "Chuyển kho"]));
    }
}
