using SmartInventoryManager.UI.Components;
namespace SmartInventoryManager.Forms.Imports;
public partial class FrmImportDetail : Form
{
    public FrmImportDetail(UiServices services, int? id = null, bool edit = false)
    {
        InitializeComponent();
        Controls.Add(new ReceiptEditor(this, services, true, id, edit));
    }
    public FrmImportDetail()
    {
        InitializeComponent();
        ReceiptUi.Build(this, true);
    }
}
