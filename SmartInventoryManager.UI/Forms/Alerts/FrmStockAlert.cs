using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Alerts;

public partial class FrmStockAlert : Form
{
    public FrmStockAlert()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        var root = ProductUi.Layout(1, 2);
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.Controls.Add(WorkflowUi.List("FrmStockAlert",
            [("Tìm kiếm", "Nhập mã hoặc tên sản phẩm"), ("Kho", "Tất cả kho"), ("Loại cảnh báo", "Tất cả loại"), ("Xử lý", "Tất cả trạng thái")],
            ["Sản phẩm", "Kho", "Loại cảnh báo", "Số lượng hiện tại", "Ngưỡng", "Ngày tạo", "Trạng thái xử lý", "Ngày xử lý", "Người xử lý"], "bell", pendingActions: ["Đánh dấu đã xử lý"]), 0, 0);
        var detail = ProductUi.Label("Chi tiết cảnh báo\nChọn một cảnh báo để xem sản phẩm, kho, ngưỡng và thông tin xử lý khi dữ liệu đã được tải.", 10);
        detail.Padding = new Padding(16);
        // [COLOR CONFIG]: nền trắng của vùng chi tiết cảnh báo.
        detail.BackColor = SmartInventoryManager.UI.Themes.AppColors.Surface;
        root.Controls.Add(detail, 0, 1);
        Controls.Add(root);
    }
}
