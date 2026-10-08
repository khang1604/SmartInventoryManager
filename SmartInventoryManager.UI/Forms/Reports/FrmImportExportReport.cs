using SmartInventoryManager.UI.Components;

namespace SmartInventoryManager.Forms.Reports;

public partial class FrmImportExportReport : Form
{
    public FrmImportExportReport(UiServices services)
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(ConnectedPages.ImportExportReport(services));
    }

    public FrmImportExportReport()
    {
        InitializeComponent();
        WorkflowUi.StyleForm(this);
        Controls.Add(WorkflowUi.List("FrmImportExportReport",
            [("Kho", "Tất cả kho"), ("Thời gian", "Chọn khoảng thời gian"), ("Loại phiếu", "Nhập và xuất")],
            ["Ngày", "Mã phiếu", "Loại phiếu", "Kho", "Sản phẩm", "Số lượng", "Đơn giá (đ)", "Thành tiền (đ)"], "bar-chart", pendingActions: ["Xem báo cáo", "Xuất Excel", "Xuất PDF"], report: true));
    }
}
