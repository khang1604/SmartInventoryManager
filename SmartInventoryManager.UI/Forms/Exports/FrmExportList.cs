using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Exports;

public partial class FrmExportList : Form
{
    // Khởi tạo màn hình danh sách phiếu xuất.
    public FrmExportList(UiServices services)
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(new ReceiptListView(services, false));
    }
    // Khởi tạo màn hình danh sách phiếu xuất.
    public FrmExportList()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmExportList",
            [("Tìm kiếm", "Nhập mã phiếu"), ("Kho", "Tất cả kho"), ("Thời gian", "Chọn khoảng thời gian"), ("Trạng thái", "Tất cả trạng thái")],
            ["Mã phiếu", "Kho", "Người lập", "Ngày xuất", "Tổng tiền (đ)", "Trạng thái"], "box-arrow-up", "Tạo phiếu xuất", () => new FrmExportDetail(), ["Sửa phiếu", "Hoàn tất", "Hủy phiếu"]));
    }
}
