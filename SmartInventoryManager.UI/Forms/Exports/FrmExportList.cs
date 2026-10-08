using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Exports;

public partial class FrmExportList : Form
{
    public FrmExportList()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmExportList",
            [("Tìm kiếm", "Nhập mã phiếu"), ("Kho", "Tất cả kho"), ("Thời gian", "Chọn khoảng thời gian"), ("Trạng thái", "Tất cả trạng thái")],
            ["Mã phiếu", "Kho", "Người lập", "Ngày xuất", "Tổng tiền (đ)", "Trạng thái"], "box-arrow-up", "Tạo phiếu xuất", () => new FrmExportDetail(), ["Sửa phiếu", "Hoàn tất", "Hủy phiếu"]));
    }
}
