using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Inventory;

public partial class FrmInventoryHistory : Form
{
    public FrmInventoryHistory()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmInventoryHistory",
            [("Sản phẩm", "Chọn sản phẩm"), ("Kho", "Tất cả kho"), ("Loại giao dịch", "Tất cả loại"), ("Thời gian", "Chọn khoảng thời gian")],
            ["Thời gian", "Sản phẩm", "Kho", "Loại giao dịch", "Số lượng biến động", "Tham chiếu", "Người thực hiện", "Ghi chú"], "file-earmark-text"));
    }
}
