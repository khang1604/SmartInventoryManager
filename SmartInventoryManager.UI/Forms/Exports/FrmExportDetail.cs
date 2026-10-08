using SmartInventoryManager.UI.Components;
namespace SmartInventoryManager.Forms.Exports;
public partial class FrmExportDetail : Form
{
    public FrmExportDetail(UiServices services, int? id = null, bool edit = false)
    {
        InitializeComponent();
        Controls.Add(new ReceiptEditor(this, services, false, id, edit));
    }
    public FrmExportDetail()
    {
        InitializeComponent();
        ReceiptUi.Build(this, false);
    }
}
