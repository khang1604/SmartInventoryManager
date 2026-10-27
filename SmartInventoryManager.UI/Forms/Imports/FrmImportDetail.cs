using SmartInventoryManager.UI.Components;
namespace SmartInventoryManager.Forms.Imports;
public partial class FrmImportDetail : Form
{
    // Khởi tạo màn hình thông tin và các dòng hàng của phiếu nhập.
    public FrmImportDetail(UiServices services, int? id = null, bool edit = false)
    {
        InitializeComponent();
        Controls.Add(new ReceiptEditor(this, services, true, id, edit));
    }
    // Khởi tạo màn hình thông tin và các dòng hàng của phiếu nhập.
    public FrmImportDetail()
    {
        InitializeComponent();
        ReceiptUi.Build(this, true);
    }
}
