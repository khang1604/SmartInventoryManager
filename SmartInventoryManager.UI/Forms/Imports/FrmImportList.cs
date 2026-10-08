using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Imports;

public partial class FrmImportList : Form
{
    public FrmImportList()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmImportList",
            [("Tìm kiếm", "Nhập mã phiếu"), ("Kho", "Tất cả kho"), ("Thời gian", "Chọn khoảng thời gian"), ("Trạng thái", "Tất cả trạng thái")],
            ["Mã phiếu", "Nhà cung cấp", "Kho", "Người lập", "Ngày nhập", "Tổng tiền (đ)", "Trạng thái"], "box-arrow-in-down", "Tạo phiếu nhập", () => new FrmImportDetail(), ["Sửa phiếu", "Hoàn tất", "Hủy phiếu"]));
    }
}
