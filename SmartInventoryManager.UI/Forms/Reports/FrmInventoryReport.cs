using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Reports;

public partial class FrmInventoryReport : Form
{
    public FrmInventoryReport()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmInventoryReport",
            [("Kho", "Tất cả kho"), ("Sản phẩm", "Chọn sản phẩm"), ("Trạng thái tồn", "Tất cả trạng thái")],
            ["Mã sản phẩm", "Tên sản phẩm", "Kho", "Đơn vị", "Số lượng tồn", "Giá trị tồn (đ)", "Trạng thái tồn"], "file-earmark-bar-graph", pendingActions: ["Xem báo cáo", "Xuất Excel", "Xuất PDF"], report: true));
    }
}
