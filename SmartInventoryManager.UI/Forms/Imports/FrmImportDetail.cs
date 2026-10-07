using SmartInventoryManager.UI.Components;
namespace SmartInventoryManager.Forms.Imports;
public partial class FrmImportDetail : Form
{
    public FrmImportDetail()
    {
        InitializeComponent();
        ReceiptUi.Build(this, true);
    }
}
