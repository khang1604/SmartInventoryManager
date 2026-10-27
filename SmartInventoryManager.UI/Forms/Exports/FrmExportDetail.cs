using SmartInventoryManager.UI.Components;
namespace SmartInventoryManager.Forms.Exports;
public partial class FrmExportDetail : Form
{
    // Khởi tạo màn hình thông tin và các dòng hàng của phiếu xuất.
    public FrmExportDetail(UiServices services, int? id = null, bool edit = false)
    {
        InitializeComponent();
        Controls.Add(new ReceiptEditor(this, services, false, id, edit));
    }
    // Khởi tạo màn hình thông tin và các dòng hàng của phiếu xuất.
    public FrmExportDetail()
    {
        InitializeComponent();
        ReceiptUi.Build(this, false);
    }
}
